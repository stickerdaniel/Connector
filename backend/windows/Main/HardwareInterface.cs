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
        event Action<HardwareInterface, string, InformationV1In> OnDeviceInformation;
        event Action<HardwareInterface, string, DataReceive> OnDeviceData;
        event Action<HardwareInterface, string, string> OnDeviceDebug;
        void RequestInformation(string id);
        void SendData(string id, DataSend data);
        void ScanForDevices();
        string ConnectionType { get; }

    }

}