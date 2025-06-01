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
    public class HardwareProtocolV1_0_0 : IHardwareProtocolVersion
    {
        public string Version { get; } = "1.0.0";
        private readonly HardwareProtocolV0_9 protocolV0_9_0 = new();

        public void Serialize(BinaryWriter writer, object message)
        {
            protocolV0_9_0.Serialize(writer, message);
        }

        public object Deserialize(BinaryReader binaryReader)
        {
            // Read former message format.
            byte[] buffer = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

            if (MatchHeader(buffer, "{\"Hand\""))
            {
                string information = Encoding.UTF8.GetString(buffer);
                // Strip the double quotes from the information json package.
                string informationV0_9_0 = Regex.Replace(information, @"""([\w]+)""", "$1");
                byte[] bufferV0_9_0 = Encoding.UTF8.GetBytes(informationV0_9_0);
                BinaryReader binaryReaderV0_9_0 = new BinaryReader(new MemoryStream(bufferV0_9_0));
                return protocolV0_9_0.Deserialize(binaryReaderV0_9_0);
            }
            else
            {
                binaryReader.BaseStream.Position = 0;
                return protocolV0_9_0.Deserialize(binaryReader);
            }
        }

        private bool MatchHeader(byte[] buffer, string header) => buffer.Take(header.Length).SequenceEqual(header.Select(b => (byte)b));
    }
}