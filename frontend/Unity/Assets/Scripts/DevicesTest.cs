using Main;
using UnityEngine;

public class DevicesTest : MonoBehaviour
{

    void Start()
    {
        Devices devices = new DevicesImpl();
        Debug.Log("Starting");
        devices.OnError += Debug.Log;
        devices.OnNewDevice += (device) =>
        {
            Debug.Log("New device " + device.Id);
            device.OnConnected += () => Debug.Log("Connected " + device.Id);
            device.OnDisconnected += () => Debug.Log("Disconnected " + device.Id);
            device.OnData += (data) => Debug.Log("Data " + device.Id + " " + data);
            device.OnError += (e) => Debug.Log("Error " + device.Id + " " + e);
            device.SendCommand(new DeviceCommand { vibration = new byte[] { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 } });
        };
        devices.Start();
        // windowsDevices.TriggerScan();
    }

}
