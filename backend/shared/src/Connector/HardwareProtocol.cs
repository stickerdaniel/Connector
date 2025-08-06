#nullable enable

using System;
using System.IO;
using System.Reflection;
using Connector.Messages;
using System.Text;
using System.Buffers.Binary;
namespace Connector
{
    /// <summary>
    /// Class to serialize and deserialize binary messages to and from the hardware.
    /// Multibyte values are serialized in big-endian order for better readability.
    /// </summary>
    public class HardwareProtocol
    {
        private const string PROTOCOL_VERSION = "2.1.0";
        private readonly IHardwareProtocolVersion protocol = new HardwareProtocolV2_1_0();
        IHardwareProtocolVersion? compatibility = null;
        public void Serialize(BinaryWriter writer, object message)
        {
            if (compatibility == null)
            {
                protocol.Serialize(writer, message);
            }
            else
            {
                compatibility.Serialize(writer, message);
            }
        }


        public object Deserialize(BinaryReader binaryReader)
        {
            IHardwareProtocolVersion[] tryProtocols = {
                    new HardwareProtocolV2_1_0(),
                    new HardwareProtocolV2_0_0(),
                    new HardwareProtocolV1_0_0()
                };
            foreach (IHardwareProtocolVersion protocol in tryProtocols)
            {
                try
                {
                    binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    object message = protocol.Deserialize(binaryReader);
                    this.compatibility = protocol;
                    return message;
                }
                catch (Exception)
                {
                    // Ignore compatibility exceptions and try the next protocol.
                }
            }
            // Get start of message for reporting.
            binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] head = new byte[10];
            binaryReader.Read(head, 0, head.Length);
            string headHex = BitConverter.ToString(head).Replace("-", " ");
            string headString = Encoding.UTF8.GetString(head);
            string errorMessage = $"Failed to deserialize message.\nFirst 10 bytes: [{headHex}]\nDecoded: '{headString}'.";
            throw new ArgumentException(errorMessage);
        }
    }
}