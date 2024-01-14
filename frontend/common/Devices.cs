// cf. https://github.com/Cynteract/Glove2/blob/main/Assets/GloveManager.cs


namespace Cynteract.CGlove
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
		TriggerScan();
		event Action<Device> OnNewDevice;
		event Action<Exception> OnError;
	}


	interface Device
	{
		string Id { get; }
		string Version { get; }
		Dictionary<string, string> Information { get; }
		ConnectionType ConnectionType { get; }
		Dictionary<string, Any>? LastData { get; }
		bool IsConnected { get; }
		void SendData(Dictionary<string, Any>);
		event Action<Dictionary<string, Any>> OnData;
		event Action OnDisconnected;
		event Action OnConnected;
		event Action<Exception> OnError;
	}
}
