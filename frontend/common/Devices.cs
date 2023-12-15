// cf. https://github.com/Cynteract/Glove2/blob/main/Assets/GloveManager.cs


namespace Cynteract.CGlove
{
	enum DeviceType
	{
		Left, Right, Beacon
	}

	enum ConnectionType { Wired, Wireless }

	abstract class Devices
	{

		public Dictionary<string, Device> Devices;
		public Device? GetDevice(DeviceType type);
		// Explicitly trigger scan. There are automatic periodic scans without calling this function.
		public TriggerScan();
		public event Action<Device> OnNewDevice;
		public event Action<Exception> OnError;
	}


	abstract class Device
	{
		public string Id;
		public Dictionary<string, string> Information;
		public ConnectionType ConnectionType;
		public Dictionary<string, Any>? LastData;
		public bool IsConnected;
		public void SendData(Dictionary<string, Any>);
		public event Action<Dictionary<string, Any>> OnData;
		public event Action OnDisconnected;
		public event Action OnConnected;
		public event Action<Exception> OnError;
	}
}
