#nullable enable
// protocol between hardware device and pc

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Main
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
        [JsonIgnore]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] header = { (byte)'D', (byte)'A', (byte)'T', (byte)'A' };
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
    };

    /// <summary> Packet format that is sent to the glove to change vibration strength or to request information on the glove. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class DataSend
    {
        [JsonIgnore]
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

    public interface Protocol
    {

        static readonly byte[] PACKAGE_DELIM = { (byte)'C', (byte)'Y', (byte)'N', (byte)'T', (byte)'E', (byte)'R', (byte)'A', (byte)'C', (byte)'T', (byte)'\n' };
        static readonly int DATA_SEND_SIZE = Marshal.SizeOf<DataSend>();
        static readonly int DATA_RECEIVE_SIZE = Marshal.SizeOf<DataReceive>();

        static void SerializeData<T>(T dataIn, byte[] dataOut)
        {
            if (dataIn == null)
                throw new System.ArgumentNullException(nameof(dataIn));
            GCHandle h = GCHandle.Alloc(dataOut, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr<T>(dataIn, h.AddrOfPinnedObject(), false);
            }
            finally
            {
                h.Free();
            }
        }
        static T DeserializeData<T>(byte[] data)
        {
            if (data == null)
                throw new System.ArgumentNullException(nameof(data));            // for serialization and deserialization of struct see https://stackoverflow.com/a/2887
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                T? deserialized = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                if (deserialized == null)
                    throw new System.ArgumentNullException(nameof(data));
                return deserialized;
            }
            finally
            {
                handle.Free();
            }
        }
        static bool MemoryCompare(byte[] sequence, byte[] array, int offset = 0)
        {
            for (int i = 0, j = offset; i < sequence.Length; i++, j++)
                if (sequence[i] != array[j])
                    return false;
            return true;
        }
    }
}
