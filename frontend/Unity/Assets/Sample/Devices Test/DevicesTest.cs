using Connector;
using UnityEngine;
using UnityEngine.UI;

public class DevicesTest : MonoBehaviour
{
    DeviceManager deviceManager = new();
    [SerializeField]
    private Button requestInformationButton;
    void Start()
    {
        Debug.Log("Starting");
        deviceManager.OnError += Debug.Log;

        deviceManager.OnDeviceConnected += (device) => 
        {
            requestInformationButton.onClick.RemoveAllListeners();
            requestInformationButton.onClick.AddListener(()=> deviceManager.RequestInformation(device.Id));
            Debug.Log("New device " + device.Id);
            device.OnData += (data) => Debug.Log("Data " + device.Id + " " + data);
            device.OnError += (e) => Debug.Log("Error " + device.Id + " " + e);
            //device.SendCommand(new DeviceCommand { vibration = new byte[] { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 } });
        };
        deviceManager.Start();
        // windowsDevices.TriggerScan();
    }
    
    void OnDisable()
    {
        deviceManager.Stop();
    }
}
