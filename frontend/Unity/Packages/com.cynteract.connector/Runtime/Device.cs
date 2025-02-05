#nullable enable

using System;
using UnityEngine;

namespace Connector
{
    public class Device
    {
        public string Id { get; private set; }
        public Information? Information { get; private set; }
        public DeviceType DeviceType { get; private set; }
        public ConnectionType ConnectionType { get; private set; }
        public Dataframe? LastData { get; private set; }
        public bool IsReady { get; private set; }


        public event Action<Dataframe>? OnData;
        public event Action<Information>? OnInformation;
        public event Action? OnReady;
        public event Action<Exception>? OnError;




        public void RaiseData(Dataframe data)
        {
            LastData=data;
            OnData?.Invoke(data);
            if (!IsReady)
            {

                if (Information != null)
                {
                    OnReady?.Invoke();
                    IsReady = true;
                }
            }
        }

        public void RaiseError(Exception e)
        {
            OnError?.Invoke(e);
        }

        public void RaiseInformation(Information information)
        {
            Debug.Log("Device received information");

            Information = information;
            DeviceType = information.hand switch
            {
                "Links" => DeviceType.Left,
                "Rechts" => DeviceType.Right,
                "Strap" => DeviceType.Strap,
                "Cushion"=>DeviceType.Cushion,
                _=> DeviceType.Unknown
            };
            OnInformation?.Invoke(information);
        }

        public Device(string id, Information? information, DeviceType deviceType, ConnectionType connectionType)
        {
            Id = id;
            Information = information;
            DeviceType = deviceType;
            ConnectionType = connectionType;
        }
    }
}
