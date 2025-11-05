using System;

namespace Connector.Messages
{
    [Serializable]
    public class Error
    {
        static Error() => Factory.RegisterMessageType("error", typeof(Error));

        public string message;
    }
}