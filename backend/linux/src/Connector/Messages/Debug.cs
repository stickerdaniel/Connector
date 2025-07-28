using System;

namespace Connector.Messages
{
    [Serializable]
    public class Debug
    {
        static Debug() => Factory.RegisterMessageType("debug", typeof(Debug));

        public string message;
    }
}