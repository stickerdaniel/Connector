#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace Connector.Messages
{
    [Serializable]
    public class Information
    {
        static Information() => Factory.RegisterMessageType("information", typeof(Information));

        public string? deviceType;
        public string? hardwareVersion;
        public string? firmwareVersion;
        public string? firmwareDate;
        public string? userType;
        public string? checkpoint;
        public string[]? vibrationPositions;
        public string[]? imuPositions;
    }
}