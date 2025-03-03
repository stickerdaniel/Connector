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
                deviceManager.SendMessage(device.Id, requestInformationMessage);

            });
            touchtexButton.onClick.AddListener(() =>
            {
                var touchtexMessage = new Connector.Messages.Touchtex
                {
                    mainVibration = 100,
                    fingerVibrations = new byte[] { 1, 2, 3, 4, 5 },
                    touchTexBoards = new Connector.Messages.TouchTexPart[] {
                        new()
                        {
                            vibrations = new byte[]
                            {
                                100,12,13,14,15
                            },
                            heat=10
                        },
                        new()
                        {
                            vibrations = new byte[]
                            {
                                21,22,23,24,25
                            },
                            heat=20
                        },
                        new()
                        {
                            vibrations = new byte[]
                            {
                                31,32,33,34,35
                            },
                            heat=30
                        },
                        new()
                        {
                            vibrations = new byte[]
                            {
                                41,42,43,44,45
                            },
                            heat=40
                        },
                        new()
                        {
                            vibrations = new byte[]
                            {
                                51,52,53,54,55
                            },
                            heat=50
                        },
                    },
                    frontPressure = 100,
                    backPressure = 100,
                };

                deviceManager.SendMessage(device.Id, touchtexMessage);
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
