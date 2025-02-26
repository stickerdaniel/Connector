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
        private const string PROTOCOL_VERSION = "2.0.0";
        ICompatibility? compatibility = null;
        public void Serialize(BinaryWriter writer, object message)
        {
            if (compatibility == null)
            {
                SerializeValue(writer, PROTOCOL_VERSION);
                SerializeValue(writer, Factory.GetMessageTypeName(message));
                SerializeValue(writer, message);
            }
            else
            {
                compatibility.Serialize(writer, message);
            }
        }


        public object Deserialize(BinaryReader binaryReader)
        {
            try
            {
                string protocolVersion = (string)DeserializeValue(binaryReader, typeof(string));
                if (protocolVersion != PROTOCOL_VERSION)
                {
                    throw new Exception($"Protocol version mismatch. Expected {PROTOCOL_VERSION}, got {protocolVersion}");
                }

                string messageTypeName = (string)DeserializeValue(binaryReader, typeof(string));
                Type messageType = Factory.GetMessageType(messageTypeName);
                object message = DeserializeValue(binaryReader, messageType);

                return message;
            }
            catch (Exception e)
            {
                try
                {
                    ICompatibility compatibilityV1 = new CompatibilityV1_0_0();
                    binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    object message = compatibilityV1.Deserialize(binaryReader);
                    compatibility = compatibilityV1;
                    return message;
                }
                catch (Exception e2)
                {
                    // Get start of message for reporting.
                    binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
                    byte[] head = new byte[10];
                    binaryReader.Read(head, 0, head.Length);
                    string headHex = BitConverter.ToString(head).Replace("-", " ");
                    string headString = Encoding.UTF8.GetString(head);
                    string errorMessage = $"Failed to deserialize message.\nFirst 10 bytes: [{headHex}]\nDecoded: '{headString}'.";
                    errorMessage += $"\nOriginal exception: '{e.Message}'";
                    errorMessage += $"\nCompatibility exception: '{e2.Message}'";
                    throw new ArgumentException(errorMessage);
                }
            }
        }

        private void SerializeValue(BinaryWriter writer, object value)
        {
            if (value is string stringValue)
            {
                writer.Write(Encoding.UTF8.GetBytes(stringValue));
                writer.Write((byte)0);
            }
            else if (value is byte byteValue)
            {
                writer.Write(byteValue);
            }
            else if (value is short shortValue)
            {
                writer.Write(BinaryPrimitives.ReverseEndianness(shortValue));
            }
            else if (value is int intValue)
            {
                writer.Write(BinaryPrimitives.ReverseEndianness(intValue));
            }
            else if (value is float floatValue)
            {
                writer.Write(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(floatValue)));
            }
            else if (value.GetType().IsArray)
            {
                Array array = (Array)value;
                writer.Write((byte)array.Length);
                foreach (object o in array)
                {
                    SerializeValue(writer, o);
                }
            }
            else if (value.GetType().IsClass)
            {
                FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(value)!;
                    SerializeValue(writer, fieldValue);
                }
            }
            else
            {
                throw new NotImplementedException($"Serialization of type {value.GetType()} is not implemented");
            }
        }
        private object DeserializeValue(BinaryReader reader, Type type)
        {

            if (type == typeof(string))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte b;
                    while ((b = reader.ReadByte()) != 0)
                    {
                        ms.WriteByte(b);
                    }
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            else if (type == typeof(byte))
            {
                return reader.ReadByte();
            }
            else if (type == typeof(short))
            {
                return BinaryPrimitives.ReverseEndianness(reader.ReadInt16());
            }
            else if (type == typeof(int))
            {
                return BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
            }
            else if (type == typeof(float))
            {
                return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(reader.ReadInt32()));
            }
            else if (type.IsArray)
            {
                int arraySize = reader.ReadByte();
                Array array = Array.CreateInstance(type.GetElementType()!, arraySize);
                for (int i = 0; i < array.Length; i++)
                {
                    array.SetValue(DeserializeValue(reader, type.GetElementType()!), i);
                }
                return array;
            }
            else if (type.IsClass)
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                object instance = Activator.CreateInstance(type)!;
                foreach (FieldInfo field in fields)
                {
                    object value = DeserializeValue(reader, field.FieldType);
                    field.SetValue(instance, value);
                }
                return instance;
            }
            else
            {
                throw new NotImplementedException($"Deserialization of type {type} is not implemented");
            }
        }

    }
}