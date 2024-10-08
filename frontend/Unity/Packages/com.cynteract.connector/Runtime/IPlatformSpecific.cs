using Connector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlatformSpecific
{
    void SendMessage(Message message);
    void Start(DeviceManager deviceManager);
    void Stop();
}
