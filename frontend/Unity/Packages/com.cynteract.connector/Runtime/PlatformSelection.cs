using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Connector {
    public static class PlatformSelection
    {
        public static IPlatformSpecific GetPlatformSpecific()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return new WindowsPlatformSpecific();
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return new MacOSPlatformSpecific();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidPlatformSpecific();
#else
            throw new System.PlatformNotSupportedException("Current platform is not supported");
#endif
        }
    }
}
