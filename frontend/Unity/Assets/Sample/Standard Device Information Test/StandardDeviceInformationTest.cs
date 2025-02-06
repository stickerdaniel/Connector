using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Connector { 
public class StandardDeviceInformationTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
            var info = new StandardDeviceInformation()
            {
                DeviceInformation = new()
                {
                    {"COM12",new Information() }
                }
            };
            var infoManager = new StandardDeviceInformationManager(Path.Combine(Application.persistentDataPath, "StandardDeviceInformation.json"));
            infoManager.Init();
            infoManager.UpdateInformation("COM4", new Information());
            print(JsonHelper.ToJson(infoManager.LoadInformation("COM4")));
            //var loadedInfo=infoManager.LoadInformation();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
}
