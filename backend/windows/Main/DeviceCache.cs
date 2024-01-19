#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Windows.Services.Maps;

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
            public required string id;
            public required string connectionType;
            public required bool isConnected;
            public string? version;
            public object? information;
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
        public void HWOnDeviceConnected(HardwareInterface sender, string id)
        {
            if (!devices.ContainsKey(id))
            {
                devices[id] = new Device()
                {
                    id = id,
                    connectionType = sender.ConnectionType,
                    isConnected = true,
                };
            }
            else
            {
                // TODO handle change of hardware interface (usb <-> bluetooth)
                devices[id].isConnected = true;
                if (devices[id].information == null)
                    sender.RequestInformation(id);
                else
                    OnMessageOut?.Invoke(new Message(Message.Types.Connect, devices[id]));
            }
        }
        public void HWOnDeviceDisconnected(HardwareInterface sender, string id)
        {
            if (devices.ContainsKey(id))
            {
                devices[id].isConnected = false;
                OnMessageOut?.Invoke(new Message(Message.Types.Disconnect, devices[id]));
            }
        }
        public void HWOnDeviceError(HardwareInterface sender, string id, string error)
        {
            OnMessageOut?.Invoke(new Message(Message.Types.Error, new { id, error }));
        }
        public void HWOnDeviceInformation(HardwareInterface sender, string id, string information)
        {
            if (devices.ContainsKey(id))
            {
                if (devices[id].information == null)
                {
                    var json = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(information)!;
                    devices[id].version = json.GetValueOrDefault("version", "1");
                    devices[id].information = information;
                    OnMessageOut?.Invoke(new Message(Message.Types.Connect, devices[id]));
                }
                else
                    devices[id].information = information;
            }
        }
        public void HWOnDeviceData(HardwareInterface sender, string id, DataReceive data)
        {
            OnMessageOut?.Invoke(new Message(Message.Types.Data, new { id, data }));
        }
        public void HWOnDeviceDebug(HardwareInterface sender, string id, string debug)
        {
            OnMessageOut?.Invoke(new Message(Message.Types.Debug, new { id, debug }));
        }

        public void OnMessageIn(Message message)
        {
            var body = (JsonElement)message.Body;
            switch (message.Type)
            {
                case Message.Types.Scan:
                    if (body.GetString() == "usb")
                        hwInterfaces.usb.StartScan();
                    // TODO bluetooth
                    // hwInterfaces.bluetooth.StartScan();
                    break;
                case Message.Types.Data:
                    string deviceId = body.GetProperty("id").GetString()!;
                    if (devices.ContainsKey(deviceId) && devices[deviceId].isConnected && devices[deviceId].connectionType == ConnectionType.Usb)
                    {
                        var options = new JsonSerializerOptions
                        {
                            IncludeFields = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        };
                        hwInterfaces.usb.SendData(deviceId, body.GetProperty("data").Deserialize<DataSend>(options)!);
                    }
                    break;
                default:
                    throw new Exception("Unknown message id: " + message.Type);
            }
        }
        public event Action<Message>? OnMessageOut;
    }
}