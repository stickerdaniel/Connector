#nullable enable

using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Connector.Messages;
using System.Text;
using System.Text.RegularExpressions;

namespace Connector
{
    /// <summary> This version adds back the double quotes to the information json package. </summary>
    public class HardwareProtocolV2_1_0 : IHardwareProtocolVersion
    {
        public string Version { get; } = "2.1.0";
        private readonly HardwareProtocolV2_0_0 protocolV2_0_0 = new();

        public void Serialize(BinaryWriter writer, object message)
        {
            protocolV2_0_0.SerializeValue(writer, Version);
            protocolV2_0_0.SerializeValue(writer, Factory.GetMessageTypeName(message));
            protocolV2_0_0.SerializeValue(writer, message);
        }

        public object Deserialize(BinaryReader binaryReader)
        {
            string protocolVersion = (string)protocolV2_0_0.DeserializeValue(binaryReader, typeof(string));
            if (protocolVersion != Version)
            {
                throw new ArgumentException($"Protocol version mismatch. Expected {Version}, got {protocolVersion}");
            }

            string messageTypeName = (string)protocolV2_0_0.DeserializeValue(binaryReader, typeof(string));
            Type messageType = Factory.GetMessageType(messageTypeName);
            object message = protocolV2_0_0.DeserializeValue(binaryReader, messageType);

            return message;
        }
    }
}