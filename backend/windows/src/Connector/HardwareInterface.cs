using System;

namespace Connector
{
    public class ConnectionType
    {
        public const string
            Usb = "usb",
            Bluetooth = "bluetooth"
        ;
    }
    public interface HardwareInterface
    {

        event Action<HardwareInterface, string> OnDeviceConnected;
        event Action<HardwareInterface, string> OnDeviceDisconnected;
        event Action<HardwareInterface, string, string> OnDeviceError;
        event Action<HardwareInterface, string, object> OnDeviceMessage;
        void SendMessage(string deviceId, object message);
        void ScanForDevices();
        string ConnectionType { get; }

    }

}