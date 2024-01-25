using Main;
using UnityEngine;

public class DevicesTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        WindowsDevices windowsDevices = new WindowsDevices();
        Debug.Log("Starting");
        windowsDevices.OnError += Debug.Log;
        windowsDevices.OnNewDevice += (device) =>
        {
            Debug.Log("New device " + device.Id);
            device.OnConnected += () => Debug.Log("Connected " + device.Id);
            device.OnDisconnected += () => Debug.Log("Disconnected " + device.Id);
            device.OnData += (data) => Debug.Log("Data " + device.Id + " " + data);
            device.OnError += (e) => Debug.Log("Error " + device.Id + " " + e);
            device.SendCommand(new DeviceCommand { vibration = new byte[] { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 } });
        };
        windowsDevices.Start();
        // windowsDevices.TriggerScan();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
