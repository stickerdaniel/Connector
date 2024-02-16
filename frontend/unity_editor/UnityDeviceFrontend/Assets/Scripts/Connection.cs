using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevicesTest1 : MonoBehaviour {

    AndroidJavaObject main;

    void Start() 
    {
        InitializeUSB();
    }

    public void InitializeUSB()
    {
        if (Application.platform == RuntimePlatform.Android) 
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");        

            main = new AndroidJavaObject("com.cynteract.connector.Main", new MessageListener());
            main.Call("initialize", unityActivity);
        }
    }

    class MessageListener : AndroidJavaProxy
    {
        public MessageListener() : base("com.cynteract.connector.MessageListener") { }

        public void onMessageIn(string message)
        {
            Debug.Log("Received callback from Android: " + message);
        }
    }
}
