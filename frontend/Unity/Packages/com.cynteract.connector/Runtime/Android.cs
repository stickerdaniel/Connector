
#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;

namespace Connector {
    class PlatformSpecific
    {

        AndroidJavaObject main;
        class MessageListener : AndroidJavaProxy
        {
            DevicesImpl devicesImpl;
            public MessageListener(DevicesImpl devicesImpl) : base("com.cynteract.connector.MessageListener")
            {
                this.devicesImpl = devicesImpl;
            }

            public void onMessageIn(string message)
            {
                devicesImpl.OnMessage(message);
            }
        }

        public void Start(DevicesImpl devicesImpl)
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // add message listener that
            main =
            main = new AndroidJavaObject("com.cynteract.connector.Main", new MessageListener(devicesImpl));
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
