using System.Text.Json;

namespace Connector.Tests;
public class SerializationTest
{
    static IEnumerable<TestCaseData> GetTestCases()
    {
        return TestData.Data.Where(entry => !entry.ContainsKey("compatibility"))
            .Select(entry => new TestCaseData(entry["name"], entry["protocol"], entry["hardwareProtocol"]));
    }

    static IEnumerable<TestCaseData> GetCompatibilityTestCases()
    {
        return TestData.Data.Where(entry => entry.ContainsKey("compatibility"))
            .Select(entry => new TestCaseData(entry["name"], entry["protocol"], entry["hardwareProtocol"], entry["compatibility"], entry["direction"]));
    }


    [Test, TestCaseSource(nameof(GetTestCases))]
    public void SerializeTest(string name, string protocolJson, string hardwareProtocolPrettyBinary)
    {
        HardwareProtocol hardwareProtocol = new();
        var (deviceId, message1) = Protocol.Deserialize(protocolJson);
        MemoryStream stream = new MemoryStream();
        hardwareProtocol.Serialize(new BinaryWriter(stream), message1);
        byte[] binary1 = stream.ToArray();
        string binary1Pretty = TestData.PrettyPrintByteArray(binary1);
        Assert.That(binary1Pretty, Is.EqualTo(hardwareProtocolPrettyBinary),
            $"Binary data is not equal:\nexpected: {hardwareProtocolPrettyBinary}\nactual: {binary1Pretty}");
        object message2 = hardwareProtocol.Deserialize(new BinaryReader(new MemoryStream(binary1)));
        string json2 = Protocol.Serialize(deviceId, message2);
        TestData.AssertJsonEquals(protocolJson, json2);
    }

    [Test, TestCaseSource(nameof(GetCompatibilityTestCases))]
    public void SerializeCompatibilityTest(string name, string protocolJson, string hardwareProtocolPrettyBinary, string compatibilityVersion, string direction)
    {
        ICompatibility compatibility = compatibilityVersion switch
        {
            "0.9.0" => new CompatibilityV0_9_0(),
            "1.0.0" => new CompatibilityV1_0_0(),
            _ => throw new NotImplementedException("Unknown compatibility version: " + compatibilityVersion)
        };
        if (direction == "toDevice")
        {
            var (deviceId, message1) = Protocol.Deserialize(protocolJson);
            MemoryStream stream = new MemoryStream();
            compatibility.Serialize(new BinaryWriter(stream), message1);
            byte[] binary1 = stream.ToArray();
            string binary1Pretty = TestData.PrettyPrintByteArray(binary1);
            Assert.That(binary1Pretty, Is.EqualTo(hardwareProtocolPrettyBinary),
                $"Binary data is not equal:\nexpected: {hardwareProtocolPrettyBinary}\nactual: {binary1Pretty}");
        }
        else if (direction == "fromDevice")
        {
            byte[] binary1 = hardwareProtocolPrettyBinary.Split(' ').Select(s => Convert.ToByte(s, 16)).ToArray();
            object message1 = new HardwareProtocol().Deserialize(new BinaryReader(new MemoryStream(binary1)));
            string json2 = Protocol.Serialize("testDevice", message1);
            TestData.AssertJsonEquals(protocolJson, json2);
        }
    }
}