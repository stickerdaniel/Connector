
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Cynteract.CGlove
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IMUData
    {
        public float x, y, z, w;
        // public static explicit operator Quaternion(IMUData data)
        // {
        //     return new Quaternion(data.x, data.y, data.z, data.w).normalized;
        // }
    };
    /// <summary> Packet format that is sent continuously by the glove to transmit force- and imu- sensor values. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class DataReceive
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] header = { (byte)'D', (byte)'A', (byte)'T', (byte)'A' };
        /// <summary> One value per force sensor, in the range [0, 1023]. </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] force;
        /// <summary> One quaternion per imu sensor, the values are in the range [-1.0, 1.0]. </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public IMUData[] imu;
        /// <summary> composite of: bb calibration profile nvs status ~ bb calibration status ~ bbbb system status </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] imuStatus;
        /// <summary> status of vibration feedback </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] vibStatus;
    };

    /// <summary> Packet format that is sent to the glove to change vibration strength or to request information on the glove. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class DataSend
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] header = { (byte)'D', (byte)'A', (byte)'T', (byte)'A' };
        /// <summary> Set the vibration strength of the vibration motors, accepted value range is [0, 100]. </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] vibration = new byte[10];
        /// <summary> Set the vibration pattern of the vibration motors. </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] vibrationPattern = new byte[10];
        /// <summary> Boolean flag to request more glove information. </summary>
        [MarshalAs(UnmanagedType.I1)]
        public bool requestInformation;
    };

    public enum PacketType { None, Data, Information, Debug };

    public class Protocol
    {

        public static readonly byte[] PACKAGE_DELIM = { (byte)'C', (byte)'Y', (byte)'N', (byte)'T', (byte)'E', (byte)'R', (byte)'A', (byte)'C', (byte)'T', (byte)'\n' };

        public static bool MemoryCompare(byte[] sequence, byte[] array, int offset = 0)
        {
            for (int i = 0, j = offset; i < sequence.Length; i++, j++)
                if (sequence[i] != array[j])
                    return false;
            return true;
        }

    }
}
