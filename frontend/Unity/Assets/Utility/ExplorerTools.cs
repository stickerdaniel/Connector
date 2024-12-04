
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
public class ExplorerTools 
{
    [MenuItem("Explorer/Open Persistant Data Path")]
    public static void OpenPersistantDataPath()
    {
        System.Diagnostics.Process p = new System.Diagnostics.Process();
        p.StartInfo = new System.Diagnostics.ProcessStartInfo(Application.persistentDataPath);
        p.Start();
    }
}
#endif
