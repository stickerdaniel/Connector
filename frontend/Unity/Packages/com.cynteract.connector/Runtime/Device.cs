#nullable enable

using System;
using UnityEngine;

namespace Connector
{
    public class Device
    {
        public string Id { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public event Action<Exception>? OnError;
        public event Action<object>? OnMessage;

        public void RaiseError(Exception e)
        {
            OnError?.Invoke(e);
        }

        public void RaiseMessage(object message)
        {
            OnMessage?.Invoke(message);
        }

        public Device(string id, ConnectionType connectionType)
        {
            Id = id;
            ConnectionType = connectionType;
        }
    }
}
