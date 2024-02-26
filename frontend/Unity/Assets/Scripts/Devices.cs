#nullable enable
// cf. https://github.com/Cynteract/Glove2/blob/main/Assets/GloveManager.cs


using System;
using System.Collections.Generic;

namespace Main
{
    enum DeviceType
    {
        Left, Right, Beacon
    }

    enum ConnectionType { Usb, Bluetooth }

    interface Devices
    {

        Dictionary<string, Device> Devices { get; }
        Device? GetDevice(DeviceType type);
        // Explicitly trigger scan. There are automatic periodic scans without calling this function.
        void TriggerScan();
        event Action<Device> OnNewDevice;
        event Action<Exception> OnError;
    }


    interface Device
    {
        string Id { get; }
        string Version { get; }
        Information Information { get; }
        DeviceType DeviceType { get; }
        ConnectionType ConnectionType { get; }
        Dataframe? LastData { get; }
        bool IsConnected { get; }
        void SendCommand(DeviceCommand data);
        event Action<Dataframe> OnData;
        event Action OnDisconnected;
        event Action OnConnected;
        event Action<Exception> OnError;
    }
}
