#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Connector.Messages;

namespace Connector
{
    public class DeviceManager
    {
        private Dictionary<string, (DateTime lastLoggedTime, int count)> messageLog = new();

        IPlatformSpecific platformSpecific = PlatformSelection.GetPlatformSpecific();

        public Dictionary<string, Device> Devices { get; private set; } = new();


        public event Action<Device>? OnDeviceConnected;
        public event Action<Device>? OnDeviceDisconnected;
        public event Action<Exception>? OnError;

        public void Start()
        {
            Devices = new();
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

        public void SendMessage(string? deviceId, object message)
        {
            try
            {
                string serializedMessage = Protocol.Serialize(deviceId, message);
                platformSpecific.SendMessage(serializedMessage);
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
                (string deviceId, object message) = Protocol.Deserialize(messageString);
                Device? device;

                switch (message)
                {
                    case Connect connectMessage:
                        if (!Devices.ContainsKey(deviceId))
                        {
                            var connectionType = connectMessage.connectionType switch
                            {
                                "usb" => ConnectionType.Usb,
                                "ble" => ConnectionType.Bluetooth,
                                _ => throw new Exception($"Unknown connection type: {connectMessage.connectionType}")
                            };
                            var newDevice = new Device(
                                                               deviceId,
                                connectionType
                                );

                            Devices.Add(deviceId, newDevice);
                            OnDeviceConnected?.Invoke(Devices[deviceId]);
                        }
                        return;
                    case Disconnect disconnectMessage:
                        if (!Devices.ContainsKey(deviceId))
                        {
                            Debug.LogError("Received disconnect for an unknown device: " + deviceId);
                            return;
                        }
                        device = Devices[deviceId];
                        OnDeviceDisconnected?.Invoke(device);
                        Devices.Remove(deviceId);
                        return;

                    case Messages.Debug debugMessage:
                        Debug.Log("Debug: " + debugMessage.message);
                        PrintMessage(debugMessage);
                        return;

                    case Error errorMessage:
                        device = Devices[deviceId];
                        Exception e = new Exception(errorMessage.message);
                        device.RaiseError(e);
                        return;

                    default:
                        device = Devices[deviceId];
                        device.RaiseMessage(message);
                        return;
                }
            }
            catch (Exception e)
            {
                var errorMessage = $"Error processing message: [{(messageString.Length > 20 ? messageString.Substring(0, 20) + "..." : messageString)}]";
                OnError?.Invoke(new Exception(errorMessage, e));
            }
        }

        /// <summary> Print debug message. Multiple messages with the same text within a short time period are collapse into print. </summary>
        private void PrintMessage(Messages.Debug debugMessage)
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

        public void TriggerScan()
        {
            object message = new Scan { connectionType = "usb" };
            SendMessage(null, message);
        }

    }
}
