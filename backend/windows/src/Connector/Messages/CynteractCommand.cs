#nullable enable

using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Connector.Messages
{
    [Serializable]
    public class Command
    {
        static Command() => Factory.RegisterMessageType("command", typeof(Command));

        public byte[]? vibrationValues;
        public byte[]? vibrationPatterns;
    }
}