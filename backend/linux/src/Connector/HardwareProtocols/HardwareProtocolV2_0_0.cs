#nullable enable

using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Connector.Messages;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Buffers.Binary;

namespace Connector
{
    public class HardwareProtocolV2_0_0 : IHardwareProtocolVersion
    {
        public string Version { get; } = "2.0.0";

        public void Serialize(BinaryWriter writer, object message)
        {
            SerializeValue(writer, Version);
            SerializeValue(writer, Factory.GetMessageTypeName(message));
            SerializeValue(writer, message);
        }

        public object Deserialize(BinaryReader binaryReader)
        {
            string protocolVersion = (string)DeserializeValue(binaryReader, typeof(string));
            if (protocolVersion != Version)
            {
                throw new ArgumentException($"Protocol version mismatch. Expected {Version}, got {protocolVersion}");
            }

            string messageTypeName = (string)DeserializeValue(binaryReader, typeof(string));
            Type messageType = messageTypeName switch
            {
                "information" => typeof(InformationV2_0_0),
                _ => Factory.GetMessageType(messageTypeName)
            };
            object message = DeserializeValue(binaryReader, messageType);

            // Transform to current message format.
            object transformedMessage = message switch
            {
                InformationV2_0_0 informationV2_0_0 => new Messages.Information
                {
                    deviceType = informationV2_0_0.deviceType,
                    hardwareVersion = "not implemented",
                    firmwareVersion = informationV2_0_0.firmwareVersion,
                    firmwareDate = informationV2_0_0.firmwareDate,
                    // users were all therapists before home version was introduced
                    userType = "pro",
                    checkpoint = informationV2_0_0.checkpoint,
                    vibrationPositions = informationV2_0_0.vibrationPositions,
                    imuPositions = informationV2_0_0.imuPositions
                },
                _ => message
            };

            return transformedMessage;
        }


        public void SerializeValue(BinaryWriter writer, object value)
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

        public object DeserializeValue(BinaryReader reader, Type type)
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

        [Serializable]
        public class InformationV2_0_0
        {
            public string? deviceType;
            public string? firmwareVersion;
            public string? firmwareDate;
            public string? checkpoint;
            public string[]? vibrationPositions;
            public string[]? imuPositions;
        }
    }
}