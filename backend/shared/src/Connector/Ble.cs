using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
#if WINDOWS
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography;
#endif

namespace Connector
{
    public class Ble : HardwareInterface
    {
        public string ConnectionType => Connector.ConnectionType.Bluetooth;

        public void SendMessage(string deviceId, object message)
        {
            // Implement the method according to your requirements
            throw new NotImplementedException();
        }
#pragma warning disable 67

        public event Action<HardwareInterface, string> OnDeviceConnected;
        public event Action<HardwareInterface, string> OnDeviceDisconnected;
        public event Action<HardwareInterface, string, string> OnDeviceError;
        public event Action<HardwareInterface, string, object> OnDeviceMessage;
#pragma warning restore 67

        public void ScanForDevices()
        {
            throw new NotImplementedException();
        }
        /*
        class BLEDevice
        {

            public string deviceId;
            // cache services and characteristics
            public GattDeviceService cynteractService = null;
            // dictionary for characteristics
            public Dictionary<string, GattCharacteristic> characteristics = new(){
                {"debug", null},
                {"data", null},
                {"information", null},
                {"send", null}
            };

            public readonly byte[] packageSendBuffer = new byte[Protocol.DATA_SEND_SIZE];
        }

        // identify by ble device id
        Dictionary<string, BLEDevice> devices = new();
        readonly DeviceWatcher deviceWatcher;


        readonly List<string> scanIds = new();




        byte[] packageSendBuffer;

        public string ConnectionType => Connector.ConnectionType.Bluetooth;

        public event Action<HardwareInterface, string> OnDeviceConnected;
        public event Action<HardwareInterface, string> OnDeviceDisconnected;
        public event Action<HardwareInterface, string, string> OnDeviceError;
        public event Action<HardwareInterface, string, string> OnDeviceInformation;
        public event Action<HardwareInterface, string, DataReceive> OnDeviceData;
        public event Action<HardwareInterface, string, string> OnDeviceDebug;


        public Ble()
        {
            packageSendBuffer = new byte[Protocol.DATA_SEND_SIZE];

            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-51208870398573714dotnet 972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;

        }
        void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (deviceInfo.Name == "CynteractGlove")
                scanIds.Add(deviceInfo.Id);
            // connectable glove
            bool isConnectable = (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
            if (scanIds.Contains(deviceInfo.Id) && isConnectable)
            {
                StartDevice(deviceInfo.Id);
                OnDeviceConnected?.Invoke(this, deviceInfo.Id);
            }

        }
        void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            bool isConnectable = (bool)deviceInfoUpdate.Properties.GetValueOrDefault("System.Devices.Aep.Bluetooth.Le.IsConnectable", false);
            // connectable flag has changed
            if (scanIds.Contains(deviceInfoUpdate.Id))
            {
                if (isConnectable)
                {
                    StartDevice(deviceInfoUpdate.Id);
                    OnDeviceConnected?.Invoke(this, deviceInfoUpdate.Id);
                }
                else
                {
                    devices.Remove(deviceInfoUpdate.Id);
                    OnDeviceDisconnected?.Invoke(this, deviceInfoUpdate.Id);
                }
            }
        }
        void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {
        }

        public void ScanForDevices()
        {
            scanIds.Clear();

            // Start the watcher. Active enumeration is limited to approximately 30 seconds.
            // This limits power usage and reduces interference with other Bluetooth activities.
            // To monitor for the presence of Bluetooth LE devices for an extended period,
            // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
            // sample for an example.
            deviceWatcher.Start();
        }
        void StartDevice(string bleId)
        {
            BLEDevice device = new BLEDevice { deviceId = bleId };
            devices.Add(bleId, device);
            Task.Run(async () =>
            {
                try
                {
                    await CacheDevice(device);
                    await SubscribeCharacteristic(device.characteristics["data"]);
                    await SubscribeCharacteristic(device.characteristics["information"]);
                    await SubscribeCharacteristic(device.characteristics["debug"]);
                }
                catch (Exception e)
                {
                    OnDeviceError?.Invoke(this, bleId, e.Message);
                }
            });
        }

        async Task CacheDevice(BLEDevice device)
        {
            if (device.cynteractService != null) return;

            // retrieve service
            BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(device.deviceId);
            GattDeviceServicesResult result = await bleDevice.GetGattServicesForUuidAsync(Guid.Parse(Config.UUID_MAP["service_cynteract"]), BluetoothCacheMode.Cached);
            if (result.Status != GattCommunicationStatus.Success)

                throw new Exception(String.Format("retrieving Service failed: {0}", result.Status));
            if (result.Services.Count == 0)

                throw new Exception(String.Format("Service was zero for : {0}", Config.UUID_MAP["service_cynteract"]));
            device.cynteractService = result.Services.First();

            // retrieve characteristics
            foreach (string characteristicId in device.characteristics.Keys)
            {
                GattCharacteristicsResult characteristicResult = await device.cynteractService.GetCharacteristicsForUuidAsync(Guid.Parse(Config.UUID_MAP[characteristicId]), BluetoothCacheMode.Cached);
                if (characteristicResult.Status != GattCommunicationStatus.Success)
                    throw new Exception(String.Format("retrieving Characteristic failed: {0}", characteristicResult.Status));
                if (characteristicResult.Characteristics.Count == 0)
                    throw new Exception(String.Format("Characteristic count zero: {0}", characteristicId));
                var characteristic = characteristicResult.Characteristics.First();
                device.characteristics[characteristicId] = characteristic;
            }
        }
        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {

            byte[] data;
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out data);
            string uuid = sender.Uuid.ToString();

            if (uuid == Config.UUID_MAP["data"])
            {
                DataReceive dataReceive = Protocol.DeserializeData<DataReceive>(data);
                OnDeviceData?.Invoke(this, sender.Service.DeviceId, dataReceive);
            }
            else if (uuid == Config.UUID_MAP["information"])
            {
                string informationReceive = Encoding.ASCII.GetString(data);
                OnDeviceInformation?.Invoke(this, sender.Service.DeviceId, informationReceive);
            }
            else if (uuid == Config.UUID_MAP["debug"])
            {
                string debugReceive = Encoding.ASCII.GetString(data);
                OnDeviceDebug?.Invoke(this, sender.Service.DeviceId, debugReceive);
            }
        }

        private async Task SubscribeCharacteristic(GattCharacteristic characteristic)
        {
            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success)
                throw new Exception(String.Format("Error subscribing to characteristic with uuid {0} and status {1}", characteristic.Uuid, status));
            characteristic.ValueChanged += Characteristic_ValueChanged;
        }

        public void RequestInformation(string id)
        {
            BLEDevice device = devices[id];
            Task.Run(async () =>
            {
                var result = await device.characteristics["information"].ReadValueAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new Exception(String.Format("Error reading information characteristic: {0}", result.Status));
                string informationReceive = Encoding.ASCII.GetString(result.Value.ToArray());
                OnDeviceInformation?.Invoke(this, id, informationReceive);
            });
        }

        public void SendData(string id, DataSend data)
        {
            BLEDevice device = devices[id];
            Task.Run(async () =>
            {
                try
                {
                    Protocol.SerializeData<DataSend>(data, device.packageSendBuffer);
                    var result = await device.characteristics["send"].WriteValueWithResultAsync(packageSendBuffer.AsBuffer());
                    if (result.Status != GattCommunicationStatus.Success)
                        throw new Exception(String.Format("Error Writing BLE Package {0}", result.Status));
                }
                catch (Exception e) { OnDeviceError?.Invoke(this, id, e.Message); }
            });
        }
        */
    }
}

// public static Dictionary<string, string> UUID_MAP = new Dictionary<string, string> {
//             {"service_cynteract", "f6f04ffa-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"data", "f6f07c3c-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"debug", "f6f07da4-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"information", "f6f07ed0-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"send", "f6f07ffc-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_thumbBase", "f6f052c0-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_thumbCenter", "f6f05414-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_thumbTop", "f6f0554a-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_indexBase", "f6f05680-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_indexCenter", "f6f05a36-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_middleBase", "f6f05b94-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_middleCenter", "f6f05cc0-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_ringBase", "f6f05dec-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_ringCenter", "f6f05f18-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_pinkyBase", "f6f06044-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_pinkyCenter", "f6f0653a-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"imu_palmCenter", "f6f066b6-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"force_thumbTop", "f6f067ec-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"force_indexTop", "f6f0690e-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"force_middleTop", "f6f06a3a-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"force_ringTop", "f6f06b5c-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"force_pinkyTop", "f6f06c92-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_palmTop", "f6f070b6-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_palmBase", "f6f07228-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_thumbTop", "f6f0735e-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_indexTop", "f6f0748a-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_middleTop", "f6f075b6-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_ringTop", "f6f076e2-9a61-11e9-a2a3-2a2ae2dbcce4"},
//             {"vib_pinkyTop", "f6f0780e-9a61-11e9-a2a3-2a2ae2dbcce4"}
//             // "f6f0811e-9a61-11e9-a2a3-2a2ae2dbcce4",
//             // "f6f08240-9a61-11e9-a2a3-2a2ae2dbcce4",
//             // "f6f08362-9a61-11e9-a2a3-2a2ae2dbcce4",
//             // "f6f086aa-9a61-11e9-a2a3-2a2ae2dbcce4",
//             // "f6f0889e-9a61-11e9-a2a3-2a2ae2dbcce4",
//             // "f6f089fc-9a61-11e9-a2a3-2a2ae2dbcce4",
//         }