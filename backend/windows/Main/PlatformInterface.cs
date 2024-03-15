// this file is synced between following two places:
//      backend\windows\Main\PlatformInterface.cs 
//      frontend\Unity\Assets\Scripts\Main\PlatformInterface.cs
//
// nullable is not used as the "required" keyword is missing in Unity (C# 9)
//
// Unity's JsonUtility needs the [Serializable] attribute
//
#nullable disable

using System;
using System.IO;

namespace Connector
{
    [Serializable]
    public class Dataframe
    {
        [Serializable]
        public struct IMUData
        {
            public float x, y, z, w;
        };
        public short[] force;
        public IMUData[] imu;
        public byte[] imuStatus;
        public byte[] vibStatus;
    }

    [Serializable]
    public class DeviceCommand
    {
        public byte[] vibration;
        public byte[] vibrationPattern;
    }

    [Serializable]
    public class Information
    {
        public string hand;
        public string version;
    }

    public class Message
    {
        public string type;

        public class Scan : Message
        {
            public string connectionType;
            public Scan() { type = "scan"; }
        }
        public class Connect : Message
        {
            public string deviceId;
            public string connectionType;
            public bool isConnected;
            public string version;
            public Information information;
            public Connect() { type = "connect"; }
        }
        public class Disconnect : Message
        {
            public string deviceId;
            public Disconnect() { type = "disconnect"; }
        }
        public class Data : Message
        {
            public string deviceId;
            public Dataframe data;
            public Data() { type = "data"; }
        }
        public class Command : Message
        {
            public string deviceId;
            public DeviceCommand command;
            public Command() { type = "command"; }
        }
        public class Debug : Message
        {
            public string deviceId;
            public string message;
            public Debug() { type = "debug"; }
        }
        public class Error : Message
        {
            public string deviceId;
            public string message;
            public Error() { type = "error"; }
        }


        private readonly static object syncLock = new object();
        public void Write(TextWriter writer)
        {
            string json = JsonHelper.ToJson(this);
            lock (syncLock)
            {
                writer.WriteLine(json);
            }
            writer.Flush();
        }

        public string ToJson()
        {
            return JsonHelper.ToJson(this);
        }

        public static Message ReadLine(TextReader reader)
        {
            string json = reader.ReadLine();
            if (json == null)
                return null;
            return FromJson(json);
        }

        public static Message FromJson(string json)
        {
            Message message = JsonHelper.FromJson<Message>(json);
            if (message == null)
                throw new Exception("Failed to deserialize message");
            switch (message.type)
            {
                case "scan":
                    message = JsonHelper.FromJson<Scan>(json);
                    break;
                case "connect":
                    message = JsonHelper.FromJson<Connect>(json);
                    break;
                case "disconnect":
                    message = JsonHelper.FromJson<Disconnect>(json);
                    break;
                case "data":
                    message = JsonHelper.FromJson<Data>(json);
                    break;
                case "command":
                    message = JsonHelper.FromJson<Command>(json);
                    break;
                case "debug":
                    message = JsonHelper.FromJson<Debug>(json);
                    break;
                case "error":
                    message = JsonHelper.FromJson<Error>(json);
                    break;
                default:
                    throw new Exception("Unknown message type: " + message.type);
            }
            return message;
        }
    }

}