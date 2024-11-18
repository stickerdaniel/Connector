using Connector;
using UnityEngine;
using UnityEngine.UI;

public class DevicesTest : MonoBehaviour
{
    IDeviceManager devices = Devices.GetManager();
    [SerializeField]
    private Button requestInformationButton;
    void Start()
    {
        Debug.Log("Starting");
        devices.OnError += Debug.Log;

        devices.OnNewDevice += (device) => 
        {
            requestInformationButton.onClick.RemoveAllListeners();
            requestInformationButton.onClick.AddListener(()=>devices.RequestInformation(device.Id));
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
    
    void OnDisable()
    {
        devices.Stop();
    }
}
