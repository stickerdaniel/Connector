#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Connector.Messages
{
    [Serializable]
    public class Touchtex
    {
        static Touchtex() => Factory.RegisterMessageType("touchtex", typeof(Touchtex));

        public string? vibrationMode;
        public int mainVibration;
        public int[]? fingerVibrations;
        public int[]? armVibrations;
        public byte heat;
    }
}
