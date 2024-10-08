using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Connector {
    public static class PlatformSelection
    {
        public static IPlatformSpecific GetPlatformSpecific()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return new WindowsPlatformSpecific();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidPlatformSpecific();
#endif
        }
    }
}
