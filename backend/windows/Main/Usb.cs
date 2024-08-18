using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using Windows.Networking;

namespace Connector
{

    public class Usb : HardwareInterface
    {

        // identify by portName for now
        Dictionary<string, UsbDevice> devices = new();
        readonly ManagementEventWatcher watcher = new();

        public string ConnectionType => Connector.ConnectionType.Usb;
        public event Action<HardwareInterface, string> OnDeviceConnected;
        public event Action<HardwareInterface, string> OnDeviceDisconnected;
        public event Action<HardwareInterface, string, string> OnDeviceError;
        public event Action<HardwareInterface, string, InformationV1In> OnDeviceInformation;
        public event Action<HardwareInterface, string, DataReceive> OnDeviceData;
        public event Action<HardwareInterface, string, string> OnDeviceDebug;

        Thread serviceThread; 
        ConcurrentQueue<Action> serviceQueue = new();
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

            serviceThread = new Thread(ServiceRoutine);
            serviceThread.Start();
            // trigger initial scan
            EnqueueScan();
        }
        private void UsbDevicePlugged(object sender, EventArrivedEventArgs args)
        {
            // rescan usb devices
            EnqueueScan();
        }
        /// <summary>
        /// Enqueues a ScanForDevices in the ServiceRoutine
        /// When a Device has an error disconnected it requests a new Scan. But this scan has the potential to close the device and thereby the thread that this method is called from. To avoid looping dependencies, an action queue was added.
        /// </summary>
        private void EnqueueScan()
        {
            serviceQueue.Enqueue(ScanForDevices);
        }
        private void ServiceRoutine()
        {
            while (true)
            {
                try
                {
                    while (!serviceQueue.IsEmpty)
                    {
                        if (serviceQueue.TryDequeue(out Action serviceAction))
                        {
                            serviceAction?.Invoke();
                        }
                    }
                }
                catch (Exception ex) {

                    OnDeviceError?.Invoke(this, "", ex.Message);
                
                }
            }
        }
        public void ScanForDevices()
        {
            HashSet<string> ports = new();
            // Use WMI to get the PNPDeviceID of each COM port
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from WIN32_SerialPort");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                string pnpDeviceID = queryObj["PNPDeviceID"].ToString();
                string portName = queryObj["DeviceID"].ToString();
                if (
                    //V2
                    (pnpDeviceID.Contains("VID_10C4") && pnpDeviceID.Contains("PID_EA60"))
                    ||
                    //V3
                    (pnpDeviceID.Contains("VID_303A") && pnpDeviceID.Contains("PID_1001"))
                    )
                {

                    ports.Add(portName);
                }

            }
            foreach (var portName in ports.Except(devices.Keys))
            {
                AddDevice(portName);
            }

            foreach (var portName in devices.Keys.Except(ports))
            {
                RemoveDevice(portName);
            }
        }

        private void AddDevice(string portName)
        {
            UsbDevice device = new UsbDevice(portName);

            device.OnDeviceData += DeviceOnDeviceData;
            device.OnDeviceError += DeviceOnDeviceError;
            device.OnDeviceInformation += DeviceOnDeviceInformation;
            device.OnDeviceDebug += DeviceOnDeviceDebug;
            device.OnRequestConnectionCheck += RecheckConnection;


            devices.Add(portName, device);
            device.Start();
            OnDeviceConnected?.Invoke(this, portName);
        }

        private void RecheckConnection(string portName)
        {
            EnqueueScan();
        }

        private void RemoveDevice(string portName)
        {
            UsbDevice device = devices[portName];

            device.OnDeviceData -= DeviceOnDeviceData;
            device.OnDeviceError -= DeviceOnDeviceError;
            device.OnDeviceInformation -= DeviceOnDeviceInformation;
            device.OnDeviceDebug -= DeviceOnDeviceDebug;
            device.OnRequestConnectionCheck -= RecheckConnection;

            device.Close();

            OnDeviceDisconnected?.Invoke(this, portName);
            devices.Remove(portName);
        }

        private void DeviceOnDeviceDebug(string portName, string message)
        {
            OnDeviceDebug?.Invoke(this, portName, message);
        }

        private void DeviceOnDeviceInformation(string portName, InformationV1In information)
        {
            OnDeviceInformation?.Invoke(this, portName, information);
        }

        private void DeviceOnDeviceError(string portName, string errorMessage)
        {
            OnDeviceError?.Invoke(this, portName, errorMessage);
        }

        private void DeviceOnDeviceData(string portName, DataReceive data)
        {
            OnDeviceData?.Invoke(this, portName, data);
        }

        public void RequestInformation(string id)
        {
            devices[id].RequestInformation();
        }

        public void SendData(string id, DataSend data)
        {
            devices[id].SendData(data);
        }
    }
}