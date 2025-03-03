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
        public class TouchtexPart
        {
            public byte[]? vibrations;
            public byte heat;
        }
        public byte mainVibration;
        public byte[]? fingerVibrations;
        public TouchtexPart[]? touchTexBoards;
        public byte frontPressure;
        public byte backPressure;
    }
}
