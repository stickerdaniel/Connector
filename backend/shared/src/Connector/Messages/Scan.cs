using System;

namespace Connector.Messages
{
    [Serializable]
    public class Scan
    {
        static Scan() => Factory.RegisterMessageType("scan", typeof(Scan));

        public string connectionType;
    }
}