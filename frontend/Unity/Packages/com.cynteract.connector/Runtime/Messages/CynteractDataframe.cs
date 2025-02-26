#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Connector.Messages
{

    [Serializable]
    public class Dataframe
    {
        static Dataframe() => Factory.RegisterMessageType("dataframe", typeof(Dataframe));

        public class IMUData
        {
            public float x, y, z, w;
        };

        /// <summary> One value per force sensor, in the range [0, 1023]. </summary>
        public short[]? forceValues;
        /// <summary> One quaternion per imu sensor, the values are in the range [-1.0, 1.0]. </summary>
        public IMUData[]? imuValues;
        /// <summary> composite of: bb calibration profile nvs status ~ bb calibration status ~ bbbb system status </summary>
        public byte[]? imuStates;
        /// <summary> status of vibration feedback </summary>
        public byte[]? vibrationStates;
    };
}
