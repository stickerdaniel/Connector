using UnityEngine;
namespace Connector
{
    class AndroidPlatformSpecific : IPlatformSpecific
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
            main = new AndroidJavaObject("com.cynteract.connector.Main", new MessageListener(deviceManager));
            main.Call("initialize", unityActivity);
        }

        public void Stop()
        {
            if (main == null)
            {
                Debug.LogWarning("Connector not running, nothing to stop");
                return;
            }
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            main.Call("close", unityActivity);
        }

        public void SendMessage(string serializedMessage)
        {
            main.Call("sendMessage", serializedMessage);
        }
    }
}
