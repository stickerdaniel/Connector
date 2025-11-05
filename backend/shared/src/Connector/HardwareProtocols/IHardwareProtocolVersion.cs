
using System.IO;

namespace Connector
{
    public interface IHardwareProtocolVersion
    {
        abstract string Version { get; }
        abstract void Serialize(BinaryWriter writer, object message);
        abstract object Deserialize(BinaryReader binaryReader);
    }
}