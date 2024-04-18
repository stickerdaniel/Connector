using System;
using System.Collections.Generic;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Linq;
using Windows.Security.Cryptography;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;

namespace Connector
{
    public class Ble : HardwareInterface
    {
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
        public event Action<HardwareInterface, string, InformationV1In> OnDeviceInformation;
        public event Action<HardwareInterface, string, DataReceive> OnDeviceData;
        public event Action<HardwareInterface, string, string> OnDeviceDebug;


        public Ble()
        {
            packageSendBuffer = new byte[Protocol.DATA_SEND_SIZE];

            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

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

        public void StartScan()
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
                if (data.Length > 512)
                    throw new ArgumentOutOfRangeException("please keep your ble package at a size of maximum 512, cf. spec!");
                string informationReceive = Encoding.ASCII.GetString(data);
                InformationV1In information = JsonHelper.FromJson<InformationV1In>(informationReceive);
                OnDeviceInformation?.Invoke(this, sender.Service.DeviceId, information);
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
                InformationV1In information = JsonHelper.FromJson<InformationV1In>(informationReceive);
                OnDeviceInformation?.Invoke(this, id, information);
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
    }
}
