using System;

namespace Connector.Messages
{
    [Serializable]
    public class Connect
    {
        static Connect() => Factory.RegisterMessageType("connect", typeof(Connect));

        public string connectionType;
    }
}