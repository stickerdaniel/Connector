#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;


#if UNITY_ANDROID

class PlatformSpecific
{

    AndroidJavaObject main;
    class MessageListener : AndroidJavaProxy
    {
        public MessageListener() : base("com.cynteract.connector.MessageListener") { }

        public void onMessageIn(string message)
        {
            Debug.Log("Received callback from Android: " + message);
        }
    }

    public void Start()
    {
        AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

        main = new AndroidJavaObject("com.cynteract.connector.Main", new MessageListener());
        main.Call("initialize", unityActivity);
    }

    public void Stop()
    {
    }

    public void SendMessage(string message)
    {
        main.Call("sendMessage", message);
    }
}

#elif UNITY_EDITOR

using System.Diagnostics;
class PlatformSpecific
{
    Process? process;

    public void Start(DevicesImpl devicesImpl)
    {
        process = new Process();
        process.StartInfo.FileName = Application.dataPath + "./Connector_bin/Connector.exe";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        // keep terminal window open in case Unity doesn't stop the process
        process.StartInfo.CreateNoWindow = false;

        // process.OutputDataReceived += OnMessage;
        process.OutputDataReceived += (sender, args) =>
        {
            // end of stream reached, backend has stopped
            if (args.Data == null)
                return;

            devicesImpl.OnMessage(sender, args.Data);
        }
        process.ErrorDataReceived += (sender, args) => devicesImpl.OnError?.Invoke(new Exception(args.Data));

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    public void Stop()
    {
        if (process != null && !process.HasExited)
        {
            process.Kill();
            process.WaitForExit();
            process = null;
        }
    }

    public void SendMessage(Message message)
    {
        if (process == null || process.HasExited)
            throw new Exception("Backend is not running");

        message.Write(process.StandardInput);
    }
}

#endif

namespace Main
{
    [Serializable]
    class DevicesImpl : Devices
    {
        [Serializable]
        public class DeviceImpl : Device
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public Information Information { get; set; }
            public DeviceType DeviceType { get; set; }
            public ConnectionType ConnectionType { get; set; }
            public Dataframe? LastData { get; set; }
            public bool IsConnected { get; set; }

            public event Action<Dataframe>? OnData;
            public event Action? OnDisconnected;
            public event Action? OnConnected;
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

            public DeviceImpl(string id, string version, Information information, DeviceType deviceType, ConnectionType connectionType, bool isConnected, Action<DeviceCommand> sendData)
            {
                Id = id;
                Version = version;
                Information = information;
                DeviceType = deviceType;
                ConnectionType = connectionType;
                IsConnected = isConnected;
                this.sendData = sendData;
            }
        }

        PlatformSpecific platformSpecific = new PlatformSpecific();

        readonly Dictionary<string, Device> devices = new Dictionary<string, Device>();

        public Dictionary<string, Device> Devices
        {
            get => devices;
            set => throw new NotImplementedException();
        }


        public event Action<Device>? OnNewDevice;
        public event Action<Exception>? OnError;

        public void Start()
        {
            platformSpecific.Start(this);
        }

        public void Stop()
        {
            platformSpecific.Stop();
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

        void OnMessage(object sender, string messageString)
        {
            try
            {
                Debug.Log(messageString);
                // deserialize twice as described in https://docs.unity3d.com/2020.1/Documentation/Manual/JSONSerialization.html
                Message message = Message.FromJson(messageString);
                DeviceImpl? device = null;


                switch (message)
                {
                    case Message.Connect connectMessage:
                        device = (DeviceImpl)devices[connectMessage.deviceId];
                        if (device != null)
                        {
                            device.IsConnected = true;
                            device.RaiseConnected();
                        }
                        else
                        {
                            DeviceType deviceType = connectMessage.information.Hand switch
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

                            device = new DeviceImpl(
                                id: connectMessage.deviceId,
                                version: connectMessage.version,
                                information: connectMessage.information,
                                deviceType: deviceType,
                                connectionType: connectionType,
                                isConnected: true,
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
                            OnNewDevice?.Invoke(device);
                            device.RaiseConnected();
                        }
                        break;

                    case Message.Disconnect disconnectMessage:
                        device = (DeviceImpl)devices[disconnectMessage.deviceId];
                        if (device == null)
                            throw new Exception("Received disconnect for an unknown device: " + disconnectMessage.deviceId);

                        device.IsConnected = false;
                        device.RaiseDisconnected();
                        break;

                    case Message.Data dataMessage:
                        device = (DeviceImpl)devices[dataMessage.deviceId];
                        if (device == null)
                            throw new Exception("Received data for an unknown device: " + dataMessage.deviceId);
                        device.RaiseData(dataMessage.data);
                        device.LastData = dataMessage.data;
                        break;

                    case Message.Debug debugMessage:
                        Debug.Log("Debug: " + debugMessage.message);
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

        public Device? GetDevice(DeviceType type)
        {
            return devices.Values.First(device => device.DeviceType == type);
        }

        public void TriggerScan()
        {
            Message message = new Message.Scan { connectionType = "usb" };
            SendMessage(message);
        }
    }
}
