#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace Connector
{
    /// <summary>
    /// Intermediate to the frontend:
    /// - proxies events from HardwareInterface
    /// - request and cache information for devices
    /// - recognize device on reconnect
    /// - determine connection type
    /// </summary>
    public class DeviceCache
    {
        public event Action<string, object>? OnMessage;

        class Device
        {
            // use hwid for now; should be replaced with a serial id
            public required string deviceId;
            public required string connectionType;
        }
        Dictionary<string, Device> devices = new();

        class HWInterfaces
        {
            public required HardwareInterface usb;
            public required HardwareInterface bluetooth;
            public IEnumerator<HardwareInterface> GetEnumerator()
            {
                yield return usb;
                yield return bluetooth;
            }
        }
        readonly HWInterfaces hwInterfaces;

        public DeviceCache(HardwareInterface usb, HardwareInterface bluetooth)
        {
            hwInterfaces = new HWInterfaces
            {
                usb = usb,
                bluetooth = bluetooth
            };

            foreach (HardwareInterface hwi in hwInterfaces)
            {
                hwi.OnDeviceConnected += HWOnDeviceConnected;
                hwi.OnDeviceDisconnected += HWOnDeviceDisconnected;
                hwi.OnDeviceError += HWOnDeviceError;
                hwi.OnDeviceMessage += HWOnDeviceMessage;
            }
        }

        readonly object messageLock = new();
        public void HWOnDeviceConnected(HardwareInterface sender, string deviceId)
        {

            if (!devices.ContainsKey(deviceId))
            {
                devices[deviceId] = new Device()
                {
                    deviceId = deviceId,
                    connectionType = sender.ConnectionType,
                };
            }
            Device device = devices[deviceId];
            OnMessage?.Invoke(deviceId, new Messages.Connect()
            {
                connectionType = device.connectionType,
            });

        }
        public void HWOnDeviceDisconnected(HardwareInterface sender, string deviceId)
        {
            // there can be mutliple events for one disconnect
            devices.Remove(deviceId);
            OnMessage?.Invoke(deviceId, new Messages.Disconnect());
        }
        public void HWOnDeviceError(HardwareInterface sender, string deviceId, string message)
        {
            OnMessage?.Invoke(deviceId, new Messages.Error()
            {
                message = message
            });
        }
        public void HWOnDeviceMessage(HardwareInterface sender, string deviceId, object message)
        {
            OnMessage?.Invoke(deviceId, message);
        }

        public void SendMessage(string? deviceId, object message)
        {
            switch (message)
            {
                case Messages.Scan scanMessage:
                    if (scanMessage.connectionType == ConnectionType.Usb)
                        hwInterfaces.usb.ScanForDevices();
                    // TODO bluetooth
                    // hwInterfaces.bluetooth.StartScan();
                    break;
                default:
                    if (devices.TryGetValue(deviceId!, out Device? device))
                    {
                        if (device.connectionType == ConnectionType.Usb)
                        {
                            hwInterfaces.usb.SendMessage(deviceId, message);
                        }
                    }
                    break;
            }
        }
    }
}