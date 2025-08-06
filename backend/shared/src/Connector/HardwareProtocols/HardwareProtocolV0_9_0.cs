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
    public class HardwareProtocolV0_9 : IHardwareProtocolVersion
    {
        public string Version { get; } = "0.9.0";
        public void Serialize(BinaryWriter writer, object message)
        {
            // Transform message to former message format.
            CommandV0_9 transformedMessage = message switch
            {
                InformationRequest => new CommandV0_9
                {
                    requestInformation = true
                },
                Command command => new CommandV0_9
                {
                    vibration = command.vibrationValues!,
                    vibrationPattern = command.vibrationPatterns!
                },
                _ => throw new NotImplementedException("Missing compatibility for message: " + message.GetType().Name)
            };

            // Write former message format.
            byte[] buffer = new byte[Marshal.SizeOf<CommandV0_9>()];
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try { Marshal.StructureToPtr<CommandV0_9>(transformedMessage, h.AddrOfPinnedObject(), false); }
            finally { h.Free(); }
            writer.Write(buffer);
        }
        public object Deserialize(BinaryReader binaryReader)
        {
            // Read former message format.
            byte[] buffer = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

            object message;
            if (MatchHeader(buffer, "DEBUG"))
            {
                message = new Messages.Debug
                {
                    message = Encoding.UTF8.GetString(buffer["DEBUG".Length..])
                };
            }
            else if (MatchHeader(buffer, "DATA"))
            {
                GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    DataframeV0_9 dataframe = Marshal.PtrToStructure<DataframeV0_9>(handle.AddrOfPinnedObject());
                    message = dataframe!;
                }
                finally
                {
                    handle.Free();
                }
            }
            else if (MatchHeader(buffer, "{Hand"))
            {
                string information = Encoding.UTF8.GetString(buffer);
                // Re-add double quotes to the json. The double quotes are removed for a shorter package size.
                string json = Regex.Replace(information, @"[\w]+", m => $"\"{m.Value}\"");
                message = JsonHelper.FromJson<InformationV0_9>(json);
            }
            else
            {
                throw new ArgumentException("Unknown message format.");
            }


            // Transform to current message format.
            object transformedMessage = message switch
            {
                InformationV0_9 informationV0_9 => new Messages.Information
                {
                    deviceType = informationV0_9.Hand switch
                    {
                        "Rechts" => "rightGlove",
                        "Links" => "leftGlove",
                        "Cushion" => "cushion",
                        "Strap" => "strap",
                        _ => informationV0_9.Hand
                    },
                    hardwareVersion = "not implemented",
                    firmwareVersion = "not implemented",
                    firmwareDate = "not implemented",
                    userType = "pro",
                    checkpoint = "not implemented",
                    vibrationPositions = TransformIndexDictionaryToArray(informationV0_9.Vibration!),
                    imuPositions = TransformIndexDictionaryToArray(informationV0_9.IMU!),
                },
                DataframeV0_9 dataframeV0_9 => new Messages.Dataframe
                {
                    forceValues = dataframeV0_9.force,
                    imuValues = dataframeV0_9.imu.Select(imuData => new Messages.Dataframe.IMUData
                    {
                        x = imuData.x,
                        y = imuData.y,
                        z = imuData.z,
                        w = imuData.w
                    }).ToArray(),
                    imuStates = dataframeV0_9.imuStatus,
                    vibrationStates = dataframeV0_9.vibStatus
                },
                Messages.Debug debug => debug,
                _ => throw new Exception("Unknown message format.")
            };
            return transformedMessage;
        }

        private bool MatchHeader(byte[] buffer, string header) => buffer.Take(header.Length).SequenceEqual(header.Select(b => (byte)b));
        private string[] TransformIndexDictionaryToArray(Dictionary<string, string> dictionary)
        {
            int maxIndex = dictionary.Keys.Select(key => int.Parse(key)).Max();
            string[] array = new string[maxIndex + 1];
            Array.Fill(array, string.Empty);
            foreach (var kvp in dictionary)
            {
                int index = int.Parse(kvp.Key);
                array[index] = kvp.Value;
            }
            return array;
        }

        private class InformationV0_9
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
            public string? Hand;
            public Dictionary<string, string>? Vibration;
            public Dictionary<string, string>? IMU;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
        }

        /// <summary> Packet format that is sent continuously by the glove to transmit force- and imu- sensor values. </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct DataframeV0_9
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct IMUData
            {
                public float x, y, z, w;
            };
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] header;
            /// <summary> One value per force sensor, in the range [0, 1023]. </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public required short[] force;
            /// <summary> One quaternion per imu sensor, the values are in the range [-1.0, 1.0]. </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public required IMUData[] imu;
            /// <summary> composite of: bb calibration profile nvs status ~ bb calibration status ~ bbbb system status </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public required byte[] imuStatus;
            /// <summary> status of vibration feedback </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public required byte[] vibStatus;

            public DataframeV0_9()
            {
                header = new byte[] { (byte)'D', (byte)'A', (byte)'T', (byte)'A' };
                force = new short[8];
                imu = new IMUData[16];
                imuStatus = new byte[16];
                vibStatus = new byte[10];
            }
        };

        /// <summary> Packet format that is sent to the glove to change vibration strength or to request information on the glove. </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CommandV0_9
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly byte[] header;
            /// <summary> Set the vibration strength of the vibration motors, accepted value range is [0, 100]. </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] vibration;
            /// <summary> Set the vibration pattern of the vibration motors. </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] vibrationPattern;
            /// <summary> Boolean flag to request more glove information. </summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool requestInformation;

            public CommandV0_9()
            {
                header = new byte[] { (byte)'D', (byte)'A', (byte)'T', (byte)'A' };
                vibration = new byte[10];
                vibrationPattern = new byte[10];
                requestInformation = false;
            }
        }
    }
}
