using Connector;
using UnityEngine;
using UnityEngine.UI;

public class DevicesTest : MonoBehaviour
{
    DeviceManager deviceManager = new();
    [SerializeField]
    private Button requestInformationButton;
    [SerializeField]
    private Button touchtexButton;

    void Start()
    {
        Debug.Log("Starting");
        deviceManager.OnError += Debug.Log;

        deviceManager.OnDeviceConnected += (device) =>
        {
            requestInformationButton.onClick.RemoveAllListeners();
            requestInformationButton.onClick.AddListener(() =>
            {
                var requestInformationMessage = new Connector.Messages.InformationRequest();
            });
            touchtexButton.onClick.AddListener(() =>
            {
                // var touchtexMessage = new Connector.Messages.Touchtex
                // {
                //     vibrationMode = "Rtp", // Set the mode to 1
                //     mainVibration = 100,
                //     fingerVibrations = new int[] { 100, 100, 100, 100, 100 },
                //     armVibrations = new int[] { 100, 100, 100, 100, 100 },
                //     heat = 100
                // };

                // deviceManager.SendMessage(device.Id, touchtexMessage);
            });
            Debug.Log("New device " + device.Id);
            device.OnError += (e) => Debug.Log("Error " + device.Id + " " + e);
            device.OnMessage += (message) => Debug.Log("Message " + device.Id + " " + message);
        };
        deviceManager.Start();
        // windowsDevices.TriggerScan();
    }

    void OnDisable()
    {
        deviceManager.Stop();
    }
}
