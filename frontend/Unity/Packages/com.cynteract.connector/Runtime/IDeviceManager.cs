#nullable enable
// cf. https://github.com/Cynteract/Glove2/blob/main/Assets/GloveManager.cs


using System;
using System.Collections.Generic;

namespace Connector
{
    public enum DeviceType
    {
        Left, Right, Beacon
    }

    public enum ConnectionType { Usb, Bluetooth }

    public interface IDeviceManager
    {

        Dictionary<string, IDevice> Devices { get; }
        IDevice? GetDevice(DeviceType type);
        // Explicitly trigger scan. There are automatic periodic scans without calling this function.
        void TriggerScan();
        void RequestInformation(string deviceId);
        void Start();
        void Stop();
        event Action<IDevice> OnNewDevice;
        event Action<Exception> OnError;
    }


    public interface IDevice
    {
        string Id { get; }
        string Version { get; }
        Information Information { get; }
        DeviceType DeviceType { get; }
        ConnectionType ConnectionType { get; }
        Dataframe? LastData { get; }
        bool IsConnected { get; }
        bool IsReady { get; }
        void SendCommand(DeviceCommand data);
        event Action<Dataframe> OnData;
        event Action OnDisconnected;
        event Action OnConnected;
        event Action OnReady;
        event Action<Exception> OnError;
    }
}
