using System;

namespace Connector.Messages
{
    [Serializable]
    public class Disconnect
    {
        static Disconnect() => Factory.RegisterMessageType("disconnect", typeof(Disconnect));
    }
}