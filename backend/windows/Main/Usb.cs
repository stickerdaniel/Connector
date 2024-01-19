using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using System.IO.Ports;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace Main
{

    public class Usb : HardwareInterface
    {
        class PackageReadBuffer
        {
            public long transmissionStartTime = 0;
            public byte[] data = new byte[1024];
            public int offset = 0;
        }
        class UsbDevice
        {

            public SerialPort serial;
            // thread for reading the serial stream
            public Thread readThread;
            public readonly PackageReadBuffer packageReadBuffer = new();
            public readonly byte[] packageSendBuffer = new byte[Protocol.DATA_SEND_SIZE];
            public readonly object writeLock = new();

        }

        // identify by portName for now
        Dictionary<string, UsbDevice> devices = new();
        readonly ManagementEventWatcher watcher = new();

        public string ConnectionType => Main.ConnectionType.Usb;
        public event Action<HardwareInterface, string> OnDeviceConnected;
        public event Action<HardwareInterface, string> OnDeviceDisconnected;
        public event Action<HardwareInterface, string, string> OnDeviceError;
        public event Action<HardwareInterface, string, string> OnDeviceInformation;
        public event Action<HardwareInterface, string, DataReceive> OnDeviceData;
        public event Action<HardwareInterface, string, string> OnDeviceDebug;

        public void Init()
        {
            // 2: device connected
            // 3: device disconnected
            var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3")
            {
                // poll every second
                WithinInterval = new TimeSpan(0, 0, 1)
            };
            watcher.EventArrived += UsbDevicePlugged;
            watcher.Query = query;
            watcher.Start();
            // trigger initial scan
            StartScan();
        }
        private void UsbDevicePlugged(object sender, EventArrivedEventArgs args)
        {
            // rescan usb devices
            StartScan();
        }
        public void StartScan()
        {
            HashSet<string> ports = new();
            // Use WMI to get the PNPDeviceID of each COM port
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from WIN32_SerialPort");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                string pnpDeviceID = queryObj["PNPDeviceID"].ToString();
                string portName = queryObj["DeviceID"].ToString();
                if (pnpDeviceID.Contains("VID_10C4") && pnpDeviceID.Contains("PID_EA60"))
                    ports.Add(portName);

            }
            foreach (var portName in ports.Except(devices.Keys))
            {
                OnDeviceConnected?.Invoke(this, portName);
                StartDevice(portName);
            }

            foreach (var portName in devices.Keys.Except(ports))
            {
                OnDeviceDisconnected?.Invoke(this, portName);
                devices.Remove(portName);
            }
        }
        void StartDevice(string portName)
        {
            UsbDevice device = new UsbDevice();
            devices.Add(portName, device);
            device.serial = new SerialPort(portName, 230400)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            device.readThread = new Thread(() => ReadRoutine(portName));
            device.readThread.Start();
        }

        void ReadRoutine(string portName)
        {
            UsbDevice device = devices[portName];
            // (re-)open serial port on first connect and on reconnects(?)
            bool doOpen = true;
            int failureCounter = 0;

            bool quit = false;
            while (!quit)
            {
                try
                {
                    if (doOpen)
                    {
                        device.serial.Open();
                        device.serial.DiscardOutBuffer();
                        device.serial.DiscardInBuffer();
                        doOpen = false;
                    }
                    ReadPackage(portName, device.serial, device.packageReadBuffer);
                }
                catch (Exception e) when (e is ThreadInterruptedException)
                {
                    // Thread.Interrupt was called, just quit
                    break;
                }
                catch (Exception e)
                {
                    failureCounter++;
                    // too many failures in a row, report this
                    if (failureCounter >= 5)
                    {
                        OnDeviceError?.Invoke(this, portName, e.ToString());
                        failureCounter = 0;
                    }
                    Thread.Sleep(50);
                    // check if device is still connected as com device
                    StartScan();
                    // device is not connected anymore, quit
                    if (!devices.ContainsKey(portName))
                        break;

                    // stream was closed or has problems, wait for reconnection/resolution
                    if (e is not TimeoutException)
                        Thread.Sleep(500);
                }

            }
        }

        public void RequestInformation(string id)
        {
            DataSend data = new()
            {
                requestInformation = true
            };
            SendData(id, data);
        }

        public void SendData(string id, DataSend data)
        {
            UsbDevice device = devices[id];
            lock (device.writeLock)
            {
                Protocol.SerializeData<DataSend>(data, device.packageSendBuffer);
                device.serial.Write(device.packageSendBuffer, 0, device.packageSendBuffer.Length);
                device.serial.Write(Protocol.PACKAGE_DELIM, 0, Protocol.PACKAGE_DELIM.Length);
            }
        }

        void ReadPackage(string id, SerialPort serial, PackageReadBuffer buffer)
        {
            int x;
            while ((x = serial.ReadByte()) != -1)
            {
                // case 1: last package timed out
                if (buffer.transmissionStartTime > 0 && DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond > buffer.transmissionStartTime + 2000)
                {
                    Console.Error.WriteLine("package timed out");
                    // assume the start of a new package
                    buffer.transmissionStartTime = 0;
                }
                // case 2: transmission of new package
                if (buffer.transmissionStartTime == 0)
                {
                    buffer.offset = 0;
                    buffer.transmissionStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }
                // case 3: buffer overflow, this package will be corrupted
                if (buffer.offset == buffer.data.Length)
                {
                    Console.Error.WriteLine("buffer overflow");
                    // continue reading the package until the end, event if it is corrupted
                    buffer.offset = 0;
                }
                // case 4: continuation of already started package
                buffer.data[buffer.offset++] = (byte)x;
                // case 5: end of package reached
                if (buffer.offset >= Protocol.PACKAGE_DELIM.Length &&
                     Protocol.MemoryCompare(Protocol.PACKAGE_DELIM, buffer.data, buffer.offset - Protocol.PACKAGE_DELIM.Length))
                {
                    //Console.Error.WriteLine(b.offset);
                    buffer.transmissionStartTime = 0;
                    if (Protocol.MemoryCompare(Encoding.ASCII.GetBytes("DATA"), buffer.data))
                    {
                        if (buffer.offset - Protocol.PACKAGE_DELIM.Length != Protocol.DATA_RECEIVE_SIZE)
                        {
                            Console.Error.WriteLine("data packet has wrong size");
                        }
                        else
                        {
                            DataReceive dataReceive = Protocol.DeserializeData<DataReceive>(buffer.data);
                            OnDeviceData?.Invoke(this, id, dataReceive);
                        }
                    }
                    else if (Protocol.MemoryCompare(Encoding.ASCII.GetBytes("DEBUG"), buffer.data))
                    {
                        string debugReceive = Encoding.ASCII.GetString(buffer.data, 5, buffer.offset - 5 - Protocol.PACKAGE_DELIM.Length);
                        OnDeviceDebug?.Invoke(this, id, debugReceive);
                    }
                    else if (buffer.data[0] == '{')
                    {
                        // glove information sent as json
                        string informationReceive = Encoding.ASCII.GetString(buffer.data, 0, buffer.offset - Protocol.PACKAGE_DELIM.Length);
                        OnDeviceInformation?.Invoke(this, id, informationReceive);
                    }

                }
            }

        }
    }
}