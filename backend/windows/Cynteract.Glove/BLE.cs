using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Linq;
using System.Collections.Concurrent;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;

namespace Cynteract.CGlove
{
    public class BLE : GloveCommunication
    {
        // dll calls

        public static Thread scanThread;
        public static BLEScan currentScan = new BLEScan();

        public bool isConnected = false;

        public BlockingCollection<Package> packages = new BlockingCollection<Package>();

        public BLE(Glove glove) : base(glove) { packageSendBuffer = new byte[Marshal.SizeOf(dataSend)]; }

        public GattDeviceService cynteractService = null;

        byte[] packageSendBuffer;

        public class Package
        {
            public String deviceId;
            public String serviceUuid;
            public String characteristicUuid;
            public byte[] data;


        }

        public class BLEScan : Scan
        {
            internal bool cancelled = false;
            private DeviceWatcher deviceWatcher;
            List<string> gloveIds;
            public override void Cancel()
            {
                cancelled = true;
            }
            private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
            {
                if (deviceInfo.Name == "CynteractGlove")
                    gloveIds.Add(deviceInfo.Id);
                // connectable glove
                bool isConnectable = (bool)deviceInfo.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
                if (gloveIds.Contains(deviceInfo.Id) && isConnectable)
                    currentScan.Found?.Invoke(deviceInfo.Id);

            }
            private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
            {
                bool isConnectable = (bool)deviceInfoUpdate.Properties.GetValueOrDefault("System.Devices.Aep.Bluetooth.Le.IsConnectable", false);



                // connectable flag has changed

                if (gloveIds.Contains(deviceInfoUpdate.Id) && (bool)isConnectable)
                    currentScan.Found?.Invoke(deviceInfoUpdate.Id);




            }
            private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
            {

            }
            public void Start()
            {
                gloveIds = new List<string>();
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

                // Start the watcher. Active enumeration is limited to approximately 30 seconds.
                // This limits power usage and reduces interference with other Bluetooth activities.
                // To monitor for the presence of Bluetooth LE devices for an extended period,
                // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
                // sample for an example.
                deviceWatcher.Start();
            }
        }


        // don't block the thread in the Found or Finished callback; it would disturb cancelling the scan
        public static Scan ScanDevices()
        {
            // if (scanThread != null)
            //     throw new InvalidOperationException("the old scan is still running");
            currentScan.Found = null;
            currentScan.Finished = null;
            currentScan.Start();
            return currentScan;
        }

        public static void RetrieveProfile(string deviceId)
        {
            // Impl.ScanServices(deviceId);
            // Impl.Service service = new Impl.Service();
            // while (Impl.PollService(out service, true) != Impl.ScanStatus.FINISHED)
            //     Console.WriteLine("service found: " + service.uuid);
            // // wait some delay to prevent error
            // Thread.Sleep(200);
            // Impl.ScanCharacteristics(deviceId, Config.UUID_MAP["service_cynteract"]);
            // Impl.Characteristic c = new Impl.Characteristic();
            // while (Impl.PollCharacteristic(out c, true) != Impl.ScanStatus.FINISHED)
            //     Console.WriteLine("characteristic found: " + c.uuid + ", user description: " + c.userDescription);
        }

        public async Task CacheService()
        {
            if (cynteractService != null) return;

            BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(glove.bleId);
            GattDeviceServicesResult result = await device.GetGattServicesForUuidAsync(Guid.Parse(Config.UUID_MAP["service_cynteract"]), BluetoothCacheMode.Cached);
            if (result.Status != GattCommunicationStatus.Success)

                throw new Exception(String.Format("retrieving Service failed: {0}", result.Status));
            if (result.Services.Count == 0)

                throw new Exception(String.Format("Service was zero for : {0}", Config.UUID_MAP["service_cynteract"]));

            cynteractService = result.Services.First();
        }

        public async Task Subscribe()
        {
            await CacheService();

            await SubscribeCharacteristic(cynteractService, Config.UUID_MAP["debug"]);
            await SubscribeCharacteristic(cynteractService, Config.UUID_MAP["data"]);
            await SubscribeCharacteristic(cynteractService, Config.UUID_MAP["information"]);

        }
        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {

            byte[] data;
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out data);
            packages.Add(new Package { deviceId = sender.Service.DeviceId, serviceUuid = sender.Service.Uuid.ToString(), characteristicUuid = sender.Uuid.ToString(), data = data });
        }
        private async Task<GattCharacteristic> GetCharacteristic(string characteristicId)
        {
            GattCharacteristicsResult characteristicResult = await cynteractService.GetCharacteristicsForUuidAsync(Guid.Parse(characteristicId), BluetoothCacheMode.Cached);
            // GattCharacteristicsResult characteristicResult = await service.GetCharacteristicsAsync();
            if (characteristicResult.Status != GattCommunicationStatus.Success)

                throw new Exception(String.Format("retrieving Characteristic failed: {0}", characteristicResult.Status));
            if (characteristicResult.Characteristics.Count == 0)

                throw new Exception(String.Format("Characteristic count zero: {0}", characteristicId));



            var characteristic = characteristicResult.Characteristics.First();
            return characteristic;
        }
        private async Task SubscribeCharacteristic(GattDeviceService service, string characteristicId)
        {
            var characteristic = await GetCharacteristic(characteristicId);



            var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            if (status != GattCommunicationStatus.Success)
                throw new Exception(String.Format("Error subscribing to characteristic with uuid {0} and status {1}", characteristicId, status));



            characteristic.ValueChanged += Characteristic_ValueChanged;

            Console.WriteLine(String.Format("Subscribed to Characteristic {0}", characteristicId));
        }
        protected override bool Connect()
        {
            if (isConnected)
                return false;
            Console.WriteLine("retrieving ble profile...");
            RetrieveProfile(glove.bleId);
            Console.WriteLine("subscribing to characteristics...");

            var task = Subscribe();
            task.Wait();
            isConnected = true;
            return true;
        }

        private async Task WritePackageAsync()
        {

            await CacheService();

            GattCharacteristic characteristic = await GetCharacteristic(Config.UUID_MAP["send"]);

            SerializeDataSend(packageSendBuffer);
            var result = await characteristic.WriteValueWithResultAsync(packageSendBuffer.AsBuffer());
            if (result.Status != GattCommunicationStatus.Success)
                throw new Exception(String.Format("Error Writing BLE Package {0}", result.Status));
        }
        protected override bool WritePackage()
        {
            WritePackageAsync().Wait();
            return true;
        }


        protected override void ReadPackage()
        {
            Package packageReceived = packages.Take();


            if (packageReceived.characteristicUuid == Config.UUID_MAP["data"])
            {
                DeserializeDataReceived(packageReceived.data);
                lock (callbackLock)
                {
                    dataReceiveCallback(dataReceive);
                }
            }
            else if (packageReceived.characteristicUuid == Config.UUID_MAP["information"])
            {
                if (packageReceived.data.Length > 512)
                    throw new ArgumentOutOfRangeException("please keep your ble package at a size of maximum 512, cf. spec!");
                informationReceive = Encoding.ASCII.GetString(packageReceived.data);
                lock (callbackLock)
                {
                    informationReceiveCallback(informationReceive);
                }
            }
            if (packageReceived.characteristicUuid == Config.UUID_MAP["debug"])
            {
                debugReceive = Encoding.ASCII.GetString(packageReceived.data);
                CConsole.Log(debugReceive, CSubConsoleType.Glove);
                Console.WriteLine("[glove debug]" + debugReceive);
            }

        }

        public override void Reset()
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            isConnected = false;
        }

    }
}
