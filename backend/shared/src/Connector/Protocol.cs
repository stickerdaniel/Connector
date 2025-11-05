
using System;
using Connector.Messages;

namespace Connector
{
    public class Protocol
    {
        public static string Serialize(string deviceId, object message)
        {
            var header = new MessageHeader
            {
                deviceId = deviceId,
                type = Factory.GetMessageTypeName(message)
            };
            var headerContent = JsonHelper.ToJson(header).Trim(BRACES);
            var bodyContent = JsonHelper.ToJson(message).Trim(BRACES);
            if (bodyContent.Length == 0)
            {
                return $"{{{headerContent}}}";
            }
            else
            {
                return $"{{{headerContent},{bodyContent}}}";
            }
        }
        public static (string deviceId, object message) Deserialize(string json)
        {
            MessageHeader header = JsonHelper.FromJson<MessageHeader>(json);
            if (header.type == null)
            {
                throw new Exception("Missing message type.");
            }
            Type messageType = Factory.GetMessageType(header.type);
            object message = JsonHelper.FromJson(json, messageType);
            return (header.deviceId, message);
        }
        private class MessageHeader
        {
            public string deviceId;
            public string type;
        }
        private static readonly char[] BRACES = { '{', '}' };

    }
}