﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
#if WINDOWS
using System.Management;
using Microsoft.Win32;
#endif

namespace Connector
{
    public class Usb : HardwareInterface
    {
        // Supported VID/PID pairs
        private static readonly (string vid, string pid)[] SupportedDevices = new[]
        {
            ("10C4", "EA60"), // V2
            ("303A", "1001"), // V3
        };

        Dictionary<string, UsbDevice> devices = new();
#if WINDOWS
        readonly ManagementEventWatcher watcher = new();
#endif

        public string ConnectionType => Connector.ConnectionType.Usb;
        public event Action<HardwareInterface, string> OnDeviceConnected;
        public event Action<HardwareInterface, string> OnDeviceDisconnected;
        public event Action<HardwareInterface, string, string> OnDeviceError;
        public event Action<HardwareInterface, string, object> OnDeviceMessage;

        Thread serviceThread;
        ConcurrentQueue<Action> serviceQueue = new();

        public void Init()
        {
#if WINDOWS
            var query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3")
            {
                WithinInterval = new TimeSpan(0, 0, 1)
            };
            watcher.EventArrived += UsbDevicePlugged;
            watcher.Query = query;
            watcher.Start();
#endif
            serviceThread = new Thread(ServiceRoutine);
            serviceThread.IsBackground = true;
            serviceThread.Start();
            EnqueueScan();
        }

#if WINDOWS
        private void UsbDevicePlugged(object sender, EventArrivedEventArgs args)
        {
            EnqueueScan();
        }
#endif

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

        public void ScanForDevices()
        {
            var filteredPorts = new HashSet<string>(Platform.GetSupportedUsbPorts(SupportedDevices));
            foreach (var portName in filteredPorts.Except(devices.Keys))
            {
                AddDevice(portName);
            }
            foreach (var portName in devices.Keys.Except(filteredPorts))
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
#if WINDOWS
            device.OnDeviceDisconnected += EnqueueScan;
#endif
            devices.Add(portName, device);
            device.Start();
            OnDeviceConnected?.Invoke(this, portName);
        }

        private void RemoveDevice(string portName)
        {
            UsbDevice device = devices[portName];

            device.OnDeviceMessage -= DeviceOnDeviceMessage;
            device.OnDeviceError -= DeviceOnDeviceError;
#if WINDOWS
            device.OnDeviceDisconnected -= EnqueueScan;
#endif
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