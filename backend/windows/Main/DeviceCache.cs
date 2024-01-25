#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Main
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
        class Device
        {
            // use hwid for now; should be replaced with a serial id
            public required string deviceId;
            public required string connectionType;
            public required bool isConnected;
            public string? version;
            public Information? information;
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
                hwi.OnDeviceInformation += HWOnDeviceInformation;
                hwi.OnDeviceData += HWOnDeviceData;
                hwi.OnDeviceDebug += HWOnDeviceDebug;
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
                    // defer to next code block
                    isConnected = false,
                };
            }

            Device device = devices[deviceId];
            // there can be mutliple events for one connect
            if (!device.isConnected)
            {
                // TODO handle change of hardware interface (usb <-> bluetooth)
                device.isConnected = true;
                if (device.information == null)
                    sender.RequestInformation(deviceId);
                else
                    OnMessageOut?.Invoke(new Message.Connect()
                    {
                        deviceId = deviceId,
                        connectionType = device.connectionType,
                        isConnected = device.isConnected,
                        version = device.version!,
                        information = device.information!
                    });
            }
        }
        public void HWOnDeviceDisconnected(HardwareInterface sender, string deviceId)
        {
            // there can be mutliple events for one disconnect 
            if (devices.ContainsKey(deviceId) && devices[deviceId].isConnected)
            {
                devices[deviceId].isConnected = false;
                OnMessageOut?.Invoke(new Message.Disconnect() { deviceId = deviceId });
            }
        }
        public void HWOnDeviceError(HardwareInterface sender, string deviceId, string message)
        {
            OnMessageOut?.Invoke(new Message.Error()
            {
                deviceId = deviceId,
                message = message
            });
        }
        public void HWOnDeviceInformation(HardwareInterface sender, string deviceId, Information information)
        {
            if (devices.ContainsKey(deviceId))
            {
                Device device = devices[deviceId];
                if (device.information == null)
                {
                    device.information = information;
                    device.version = information.version ?? "1";
                    OnMessageOut?.Invoke(new Message.Connect()
                    {
                        deviceId = deviceId,
                        connectionType = device.connectionType,
                        isConnected = device.isConnected,
                        version = device.version!,
                        information = device.information!
                    });
                }
                else
                    device.information = information;
            }
        }
        public void HWOnDeviceData(HardwareInterface sender, string deviceId, DataReceive data)
        {
            // only propagate data after receiving information
            if (devices[deviceId].version != null)
                OnMessageOut?.Invoke(new Message.Data()
                {
                    deviceId = deviceId,
                    data = new Dataframe()
                    {
                        force = data.force,
                        imu = data.imu.Select(quaternion => new Dataframe.IMUData()
                        {
                            x = quaternion.x,
                            y = quaternion.y,
                            z = quaternion.z,
                            w = quaternion.w
                        }).ToArray(),
                        imuStatus = data.imuStatus,
                        vibStatus = data.vibStatus
                    }
                });
        }
        public void HWOnDeviceDebug(HardwareInterface sender, string deviceId, string message)
        {
            OnMessageOut?.Invoke(new Message.Debug()
            {
                deviceId = deviceId,
                message = message
            });
        }

        public void OnMessageIn(Message message)
        {
            switch (message)
            {
                case Message.Scan scanMessage:
                    if (scanMessage.connectionType == ConnectionType.Usb)
                        hwInterfaces.usb.StartScan();
                    // TODO bluetooth
                    // hwInterfaces.bluetooth.StartScan();
                    break;
                case Message.Command commandMessage:
                    string deviceId = commandMessage.deviceId!;
                    if (devices.ContainsKey(deviceId) && devices[deviceId].isConnected && devices[deviceId].connectionType == ConnectionType.Usb)
                    {
                        hwInterfaces.usb.SendData(deviceId, new DataSend()
                        {
                            vibration = commandMessage.command.vibration,
                            vibrationPattern = commandMessage.command.vibrationPattern,
                        });
                    }
                    break;
                default:
                    throw new Exception("Unknown message type: " + message.type);
            }
        }
        public event Action<Message>? OnMessageOut;
    }
}