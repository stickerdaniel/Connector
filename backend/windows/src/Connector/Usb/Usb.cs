using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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
        public event Action<HardwareInterface, string, object> OnDeviceMessage;

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
                catch (Exception ex)
                {
                    OnDeviceError?.Invoke(this, null, ex.Message);
                }
            }
        }
        private static IEnumerable<string> ListSubKeys(Microsoft.Win32.RegistryKey key)
        {
            if (key != null)
                foreach (var subKeyName in key.GetSubKeyNames())
                    yield return subKeyName;
        }
        private static IEnumerable<string> ListValues(Microsoft.Win32.RegistryKey key)
        {
            if (key != null)
                foreach (var valueName in key.GetValueNames())
                    yield return key.GetValue(valueName)?.ToString(); ;
        }

        public void ScanForDevices()
        {
            HashSet<string> ports = new();
            // Use registry to find and filter COM ports by VID/PID
            using (var serialKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            using (var usbRootKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
            {
                foreach (var portName in ListValues(serialKey))
                {
                    if (string.IsNullOrEmpty(portName))
                        continue;

                    foreach (var deviceKeyName in ListSubKeys(usbRootKey))
                    {
                        // Filter by VID/PID
                        if (
                        // V2
                        (deviceKeyName.Contains("VID_10C4") && deviceKeyName.Contains("PID_EA60")) ||
                        // V3
                        (deviceKeyName.Contains("VID_303A") && deviceKeyName.Contains("PID_1001"))
                        )
                        {
                            using (var deviceKey = usbRootKey.OpenSubKey(deviceKeyName))
                            {
                                foreach (var instanceKeyName in ListSubKeys(deviceKey))
                                {
                                    using (var deviceParameters = deviceKey.OpenSubKey($"{instanceKeyName}\\Device Parameters"))
                                    {
                                        string port = deviceParameters?.GetValue("PortName") as string;
                                        if (string.Equals(port, portName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ports.Add(portName);
                                        }
                                    }
                                }
                            }
                        }
                    }
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
            SerialPort serial = new SerialPort(portName, 230400)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            UsbDevice device = new UsbDevice(portName, new SerialPortWrapper(serial));

            device.OnDeviceMessage += DeviceOnDeviceMessage;
            device.OnDeviceError += DeviceOnDeviceError;
            // Rescan devices after clean up. The registry updates do not match the timing of the Win32_DeviceChangeEvent.
            device.OnDeviceDisconnected += EnqueueScan;

            devices.Add(portName, device);
            device.Start();
            OnDeviceConnected?.Invoke(this, portName);
        }

        private void RemoveDevice(string portName)
        {
            UsbDevice device = devices[portName];

            device.OnDeviceMessage -= DeviceOnDeviceMessage;
            device.OnDeviceError -= DeviceOnDeviceError;
            device.OnDeviceDisconnected -= EnqueueScan;

            device.Close();

            OnDeviceDisconnected?.Invoke(this, portName);
            devices.Remove(portName);
        }



        private void DeviceOnDeviceError(string portName, string errorMessage)
        {
            OnDeviceError?.Invoke(this, portName, errorMessage);
        }

        private void DeviceOnDeviceMessage(string portName, object message)
        {
            OnDeviceMessage?.Invoke(this, portName, message);
        }
        public void SendMessage(string deviceId, object message)
        {
            devices[deviceId].SendMessage(message);
        }
    }
}