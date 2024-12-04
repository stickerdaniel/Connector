#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Connector
{
    public class DeviceManager
    {
        private Dictionary<string, (DateTime lastLoggedTime, int count)> messageLog = new();

        IPlatformSpecific platformSpecific = PlatformSelection.GetPlatformSpecific();

        public Dictionary<string, Device> Devices { get; private set; } = new();

        private string jsonPath;

        public event Action<Device>? OnDeviceConnected;
        public event Action<Device>? OnDeviceDisconnected;
        public event Action<Exception>? OnError;

        public void Start()
        {
            Devices = new();
            jsonPath = Path.Combine(Application.persistentDataPath, "StandardDeviceInformation.json");

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
                // deserialize twice as described in https://docs.unity3d.com/2020.1/Documentation/Manual/JSONSerialization.html
                Message message = Message.FromJson(messageString);


                switch (message)
                {
                    case Message.Connect connectMessage:
                        if (!Devices.ContainsKey(connectMessage.deviceId))
                        {
                            var connectionType = connectMessage.connectionType switch
                            {
                                "usb" => ConnectionType.Usb,
                                "ble" => ConnectionType.Bluetooth,
                                _ => throw new Exception($"Unknown connection type: {connectMessage.connectionType}")
                            };
                            var newDevice = new Device(
                                connectMessage.deviceId,
                                null,
                                DeviceType.Unknown,
                                connectionType
                                );

                            Devices.Add(connectMessage.deviceId, newDevice);
                            OnDeviceConnected?.Invoke(Devices[connectMessage.deviceId]);
                            var standardInfo = LoadStandardDeviceInformation();
                            if (standardInfo != null)
                            {
                                Devices[connectMessage.deviceId].RaiseInformation(standardInfo);
                            }
                            
                        }
                        return;
                    case Message.Disconnect disconnectMessage:
                        if (!Devices.ContainsKey(disconnectMessage.deviceId))
                        {
                            Debug.LogError("Received disconnect for an unknown device: " + disconnectMessage.deviceId);
                            return;
                        }
                        var device = Devices[disconnectMessage.deviceId];
                        OnDeviceDisconnected?.Invoke(device);
                        Devices.Remove(disconnectMessage.deviceId);
                        return;
                    case Message.InformationMessage infoMessage:
                        if (!Devices.ContainsKey(infoMessage.deviceId))
                        {
                            Debug.LogError("Received information for an unknown device: " + infoMessage.deviceId);
                            return;
                        }
                        SaveStandardDeviceInformation(infoMessage.information);
                        Devices[infoMessage.deviceId].RaiseInformation(infoMessage.information);
                        return;
                    case Message.Data dataMessage:
                        if (!Devices.ContainsKey(dataMessage.deviceId))
                        {
                            Debug.LogError("Received data for an unknown device: " + dataMessage.deviceId);
                            return;
                        }
                        if (Devices[dataMessage.deviceId].Information == null)
                        {
                            Debug.Log("Data arrived, requesting Information");
                            RequestInformation(dataMessage.deviceId);
                        }
                        Devices[dataMessage.deviceId].RaiseData(dataMessage.data);
                        return;
                    case Message.Debug debugMessage:
                            Debug.Log("Debug: " + debugMessage.message);
                            PrintMessage(debugMessage);
                        return;
                    case Message.Error errorMessage:
                        if (!Devices.ContainsKey(errorMessage.deviceId))
                        {

                            Debug.LogError($"Received error for an unknown device ({errorMessage.deviceId}): ${errorMessage.message}");
                            return;
                        }
                        Exception e = new Exception(errorMessage.message);
                        var errorDevice = Devices[errorMessage.deviceId];
                        errorDevice.RaiseError(e);
                        return;

                    default:
                        Debug.LogError("Unknown message id: " + message.type);
                        return;
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(e);
            }
        }

        private void PrintMessage(Message.Debug debugMessage)
        {
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

        public Device? GetDevice(DeviceType type)
        {
            return Devices.Values.First(device => device.DeviceType == type);
        }

        public void TriggerScan()
        {
            Message message = new Message.Scan { connectionType = "usb" };
            SendMessage(message);
        }
        public void RequestInformation(string deviceId)
        {
            Message message = new Message.InformationRequest() { deviceId = deviceId };
            SendMessage(message);
        }
        public void SaveStandardDeviceInformation(Information information)
        {
            var informationJson=JsonHelper.ToJson(information);
            File.WriteAllText(jsonPath, informationJson);
        }
        public Information? LoadStandardDeviceInformation()
        {
            if (!File.Exists(jsonPath))
            {
                return null;
            }
            var informationJson=File.ReadAllText(jsonPath);
            return JsonHelper.FromJson<Information>(informationJson);
        }
    }
}
