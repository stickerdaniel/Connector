
#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;

namespace Connector {
    class PlatformSpecific
    {

        AndroidJavaObject main;
        class MessageListener : AndroidJavaProxy
        {
            DeviceManager deviceManager;
            public MessageListener(DeviceManager deviceManager) : base("com.cynteract.connector.MessageListener")
            {
                this.deviceManager = deviceManager;
            }

            public void onMessage(string message)
            {
                deviceManager.OnMessage(message);
            }
        }

        public void Start(DeviceManager deviceManager)
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // add message listener that
            main =
            main = new AndroidJavaObject("com.cynteract.connector.Main", new MessageListener(deviceManager));
            main.Call("initialize", unityActivity);
        }

        public void Stop()
        {
        }

        public void SendMessage(Message message)
        {
            main.Call("onCommand", message.ToJson());
        }
    }
}
#endif
