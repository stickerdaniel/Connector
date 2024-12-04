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
                };
            }
            sender.RequestInformation(deviceId);
            Device device = devices[deviceId];
            OnMessageOut?.Invoke(new Message.Connect()
            {
                deviceId = deviceId,
                connectionType = device.connectionType,
            });

        }
        public void HWOnDeviceDisconnected(HardwareInterface sender, string deviceId)
        {
            // there can be mutliple events for one disconnect 
            if (devices.ContainsKey(deviceId))
            {
                devices.Remove(deviceId);
            }
            OnMessageOut?.Invoke(new Message.Disconnect() { deviceId = deviceId });

        }
        public void HWOnDeviceError(HardwareInterface sender, string deviceId, string message)
        {
            OnMessageOut?.Invoke(new Message.Error()
            {
                deviceId = deviceId,
                message = message
            });
        }
        public void HWOnDeviceInformation(HardwareInterface sender, string deviceId, InformationV1In informationIn)
        {
            InformationV1Out informationOut = TransformInformationV1InToOut(informationIn);
            OnMessageOut?.Invoke(new Message.InformationMessage()
            {
                deviceId = deviceId,
                information = informationOut
            });
        }
        public void HWOnDeviceData(HardwareInterface sender, string deviceId, DataReceive data)
        {
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
        private static List<string> TransformIndexDictToList(Dictionary<string, string> dict)
        {
            int maxKey = dict.Keys.Select(int.Parse).Max();
            List<string> list = Enumerable.Repeat("", maxKey + 1).ToList();
            foreach (var item in dict)
                list[int.Parse(item.Key)] = item.Value;
            return list;
        }
        private static InformationV1Out TransformInformationV1InToOut(InformationV1In information)
        {
            return new InformationV1Out()
            {
                version = "1",
                hand = information.Hand,
                vibration = TransformIndexDictToList(information.Vibration),
                imu = TransformIndexDictToList(information.IMU)
            };
        }


        public void OnMessageIn(Message message)
        {
            switch (message)
            {
                case Message.Scan scanMessage:
                    if (scanMessage.connectionType == ConnectionType.Usb)
                        hwInterfaces.usb.ScanForDevices();
                    // TODO bluetooth
                    // hwInterfaces.bluetooth.StartScan();
                    break;
                case Message.Command commandMessage:
                    string deviceId = commandMessage.deviceId!;
                    if (devices.ContainsKey(deviceId) && devices[deviceId].connectionType == ConnectionType.Usb)
                    {
                        hwInterfaces.usb.SendData(deviceId, new DataSend()
                        {
                            vibration = commandMessage.command.vibration,
                            vibrationPattern = commandMessage.command.vibrationPattern,
                        });
                    }
                    break;
                case Message.InformationRequest infoRequest:
                    string infoDeviceId = infoRequest.deviceId!;
                    if (devices.ContainsKey(infoDeviceId) && devices[infoDeviceId].connectionType == ConnectionType.Usb)
                    {
                        hwInterfaces.usb.RequestInformation(infoDeviceId);
                    }
                    break;
                default:
                    throw new Exception("Unknown message type: " + message.type);
            }
        }
        public event Action<Message>? OnMessageOut;
    }
}