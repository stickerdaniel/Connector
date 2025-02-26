
using System.IO;

namespace Connector
{
    public interface ICompatibility
    {
        abstract string Version { get; }
        abstract void Serialize(BinaryWriter writer, object message);
        abstract object Deserialize(BinaryReader binaryReader);
    }
}