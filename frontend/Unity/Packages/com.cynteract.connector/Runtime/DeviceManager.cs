#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace Connector
{
    public class Devices
    {
        public static IDeviceManager GetManager()
        {
            return new DeviceManager();
        }
    }

    public class DeviceManager : IDeviceManager
    {
        private Dictionary<string, (DateTime lastLoggedTime, int count)> messageLog = new Dictionary<string, (DateTime, int)>();
        class Device : IDevice
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public Information Information { get; set; }
            public DeviceType DeviceType { get; set; }
            public ConnectionType ConnectionType { get; set; }
            public Dataframe? LastData { get; set; }
            public bool IsConnected { get; set; }
            public bool IsReady { get; set; }

            public event Action<Dataframe>? OnData;
            public event Action? OnDisconnected;
            public event Action? OnConnected;
            public event Action? OnReady;
            public event Action<Exception>? OnError;


            public void SendCommand(DeviceCommand command)
            {
                sendData?.Invoke(command);
            }

            // only called from the outer class
            public void RaiseConnected()
            {
                OnConnected?.Invoke();
            }

            public void RaiseReady()
            {
                OnReady?.Invoke();
            }

            public void RaiseDisconnected()
            {
                OnDisconnected?.Invoke();
            }

            public void RaiseData(Dataframe data)
            {
                OnData?.Invoke(data);
            }

            public void RaiseError(Exception e)
            {
                OnError?.Invoke(e);
            }

            public Action<DeviceCommand> sendData;

            public Device(string id, string version, Information information, DeviceType deviceType, ConnectionType connectionType, bool isConnected, bool isReady, Action<DeviceCommand> sendData)
            {
                Id = id;
                Version = version;
                Information = information;
                DeviceType = deviceType;
                ConnectionType = connectionType;
                IsConnected = isConnected;
                IsReady = isReady;
                this.sendData = sendData;
            }
        }

        IPlatformSpecific platformSpecific = PlatformSelection.GetPlatformSpecific();

        readonly Dictionary<string, IDevice> devices = new Dictionary<string, IDevice>();

        public Dictionary<string, IDevice> Devices
        {
            get => devices;
            set => throw new NotImplementedException();
        }


        public event Action<IDevice>? OnNewDevice;
        public event Action<Exception>? OnError;

        public void Start()
        {
            platformSpecific.Start(this);
        }

        public void Stop()
        {
            platformSpecific.Stop();
        }

        public void RaiseError(Exception e)
        {
            OnError?.Invoke(e);
        }

        void SendMessage(Message message)
        {
            try
            {
                platformSpecific.SendMessage(message);
            }
            catch (Exception e)
            {
                OnError?.Invoke(e);
            }
        }

        public void OnMessage(string messageString)
        {
            try
            {
                //Debug.Log(messageString);
                // deserialize twice as described in https://docs.unity3d.com/2020.1/Documentation/Manual/JSONSerialization.html
                Message message = Message.FromJson(messageString);
                Device? device = null;


                switch (message)
                {
                    case Message.Connect connectMessage:
                        if (devices.ContainsKey(connectMessage.deviceId))
                        {
                            device = (Device)devices[connectMessage.deviceId];
                            if (device != null)
                            {
                                device.IsConnected = true;
                                device.RaiseConnected();
                            }
                        }
                        else
                        {
                            DeviceType deviceType = connectMessage.information.hand switch
                            {
                                "Links" => DeviceType.Left,
                                "Rechts" => DeviceType.Right,
                                _ => DeviceType.Beacon
                            };
                            ConnectionType connectionType = connectMessage.connectionType switch
                            {
                                "usb" => ConnectionType.Usb,
                                "bluetooth" => ConnectionType.Bluetooth,
                                _ => throw new Exception("Unknown connection type: " + connectMessage.connectionType)
                            };

                            device = new Device(
                                id: connectMessage.deviceId,
                                version: connectMessage.version,
                                information: connectMessage.information,
                                deviceType: deviceType,
                                connectionType: connectionType,
                                isConnected: true,
                                isReady: false,
                                sendData: data =>
                                {
                                    Message message = new Message.Data()
                                    {
                                        deviceId = connectMessage.deviceId,

                                    };
                                    SendMessage(message);
                                }
                            );

                            devices.Add(connectMessage.deviceId, device);
                            Debug.Log("Invoking OnNewDevice");
                            OnNewDevice?.Invoke(device);
                            device.RaiseConnected();
                        }
                        break;

                    case Message.Disconnect disconnectMessage:
                        if (!devices.ContainsKey(disconnectMessage.deviceId))
                            throw new Exception("Received disconnect for an unknown device: " + disconnectMessage.deviceId);
                        device = (Device)devices[disconnectMessage.deviceId];
                        device.IsConnected = false;
                        device.IsReady = false;
                        device.RaiseDisconnected();
                        break;

                    case Message.Data dataMessage:
                        if (!devices.ContainsKey(dataMessage.deviceId))
                            throw new Exception("Received data for an unknown device: " + dataMessage.deviceId);
                        device = (Device)devices[dataMessage.deviceId];
                        if (!device.IsReady)
                        {
                            device.IsReady = true;
                            device.RaiseReady();
                        }
                        device.RaiseData(dataMessage.data);
                        device.LastData = dataMessage.data;
                        break;

                    case Message.Debug debugMessage:
                        {
                             Debug.Log("Debug: " + debugMessage.message);

                            int seconds = 5;
                            string messagetext = debugMessage.message;
                            DateTime now = DateTime.Now;

                            // Check if the message has been logged before and if it's within the last second
                            if (messageLog.TryGetValue(messagetext, out var logData) && (now - logData.lastLoggedTime).TotalSeconds <= seconds)
                                messageLog[messagetext] = (logData.lastLoggedTime, logData.count + 1);
                            else
                            {
                                if (logData.count > 1)
                                    Debug.Log($"Debug: {messagetext} (x{logData.count})");
                                messageLog[messagetext] = (now, 1);
                            }
                            // Periodically, outside the case block, you need to check and log any messages that were counted but not logged yet
                            // This could be done in an Update method or a separate timer-based mechanism
                            foreach (var key in messageLog.Keys.ToList())
                            {
                                var data = messageLog[key];
                                if ((now - data.lastLoggedTime).TotalSeconds > seconds && data.count > 1)
                                {
                                    Debug.Log($"Debug: {key} (x{data.count})");
                                    messageLog.Remove(key); // Reset after logging
                                }
                                else if ((now - data.lastLoggedTime).TotalSeconds > seconds)
                                {
                                    // Remove old entries that were already logged as single messages
                                    messageLog.Remove(key);
                                }
                            }
                        }
                        break;


                    case Message.Error errorMessage:
                        Exception e = new Exception(errorMessage.message);
                        if (device == null)
                            throw e;
                        device.RaiseError(e);
                        break;

                    default:
                        throw new Exception("Unknown message id: " + message.type);
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(e);
            }
        }

        public IDevice? GetDevice(DeviceType type)
        {
            return devices.Values.First(device => device.DeviceType == type);
        }

        public void TriggerScan()
        {
            Message message = new Message.Scan { connectionType = "usb" };
            SendMessage(message);
        }
        public void RequestInformation(string deviceId)
        {
            Message message = new Message.InformationRequest() { deviceId= deviceId };
            SendMessage(message);
        }
    }
}
