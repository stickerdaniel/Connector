using Connector;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StressTest : MonoBehaviour
{
    DeviceManager deviceManager = new DeviceManager();
    [SerializeField]
    private Color gloveNotConnectedColor = Color.red;
    [SerializeField]
    private Color gloveConnectedColor = Color.cyan;
    [SerializeField]
    private Color gloveReadyColor = Color.green;

    [SerializeField]
    private Image coloredDotImage;
    [SerializeField]
    private Button startRestartDeviceManagerStressTestButton;
    [SerializeField]
    private InputField delayInputField;

    [SerializeField]
    private Button requestInformationButton;

    ConcurrentQueue<Action> actionQueue = new();

    bool ready = false;
    bool connected = false;

    private void Awake()
    {
        startRestartDeviceManagerStressTestButton.onClick.AddListener(() =>
        {
            StopAllCoroutines();
            StartCoroutine(RestartDeviceManagerCoroutine());
        });
        deviceManager.OnDeviceConnected += device =>
        {
            requestInformationButton.onClick.RemoveAllListeners();
            //requestInformationButton.onClick.AddListener(() => deviceManager.RequestInformation(device.Id));
            connected = true;
            UpdateColor();
            // device.OnReady += () =>
            // {
            //     Debug.Log("Ready");
            //     ready = true;
            //     UpdateColor();
            // };
        };
        deviceManager.OnDeviceDisconnected += device =>
        {
            ready = false;
            connected = false;
            UpdateColor();
        };

    }

    IEnumerator RestartDeviceManagerCoroutine()
    {
        while (true)
        {

            Debug.Log("Stopping");
            deviceManager.Stop();
            ready = false;
            connected = false;
            UpdateColor();
            yield return new WaitForSeconds(float.Parse(delayInputField.text));
            Debug.Log("Starting");
            deviceManager.Start();
            yield return new WaitForSeconds(float.Parse(delayInputField.text));
        }
    }
    void OnEnable()
    {
        SetColor(gloveNotConnectedColor);

        //deviceManager.Start();
    }
    private void Start()
    {

    }
    private void OnDisable()
    {
        StopAllCoroutines();

        deviceManager.Stop();
    }
    private void OnDestroy()
    {
        StopAllCoroutines();

        deviceManager.Stop();
    }
    void SetColor(Color color)
    {
        actionQueue.Enqueue(() =>
        {
            if (coloredDotImage != null)
            {
                coloredDotImage.color = color;
            }
        });

    }
    void UpdateColor()
    {
        if (ready)
        {
            SetColor(gloveReadyColor);
            return;
        }
        if (connected)
        {
            SetColor(gloveConnectedColor);
            return;
        }
        SetColor(gloveNotConnectedColor);

    }
    private void Update()
    {
        while (actionQueue.Count > 0)
        {
            if (actionQueue.TryDequeue(out Action action))
            {
                action();
            }
        }
    }
}
