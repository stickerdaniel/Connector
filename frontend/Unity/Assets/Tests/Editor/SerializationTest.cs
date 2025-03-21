using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Connector;
using System.Collections.Generic;
using System.Linq;

public class SerializationTest
{
    static IEnumerable<TestCaseData> GetTestCases()
    {
        return TestData.Data.Where(entry => !entry.ContainsKey("compatibility"))
            .Select(entry => new TestCaseData(entry["name"], entry["protocol"], entry["hardwareProtocol"]));
    }

    [Test, TestCaseSource(nameof(GetTestCases))]
    public void SerializeTest(string name, string protocolJson, string hardwareProtocolPrettyBinary)
    {
        var (deviceId, message) = Protocol.Deserialize(protocolJson);
        string json2 = Protocol.Serialize(deviceId, message);
        TestData.AssertJsonEquals(protocolJson, json2);
    }

    [Test]  // Normal unit test
    public void SimpleTest()
    {
        Assert.AreEqual(2 + 2, 4);
    }

    [Test]
    public void DebugMessageTest()
    {
        var debug = new Connector.Messages.Debug { message = "Backend started." };
        string json = Protocol.Serialize("testDevice", debug);
        (string deviceId, object message) = Protocol.Deserialize(json);
        Assert.AreEqual("testDevice", deviceId);
        Assert.AreEqual("Backend started.", ((Connector.Messages.Debug)message).message);
    }

}
