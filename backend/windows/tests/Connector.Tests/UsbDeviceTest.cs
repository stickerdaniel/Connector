
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Connector.Tests;
public class UsbDeviceTest
{
    private UsbDevice usbDevice;
    private SerialPortMock serialPortMock;
    private BlockingCollection<Tuple<string, string>> errorQueue = [];
    private BlockingCollection<Tuple<string, object>> messageQueue = [];

    [SetUp]
    public void StartUsbDevice()
    {
        serialPortMock = new SerialPortMock()
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            PortName = "COM1"
        };
        usbDevice = new UsbDevice("COM1", serialPortMock);
        usbDevice.OnDeviceError += (portName, errorMessage) =>
        {
            Console.WriteLine($"Error on port {portName}: {errorMessage}");
            errorQueue.Add(new Tuple<string, string>(portName, errorMessage));
        };
        usbDevice.OnDeviceMessage += (portName, message) =>
        {
            messageQueue.Add(new Tuple<string, object>(portName, message));
        };
        usbDevice.Start();
    }

    [TearDown]
    public void StopUsbDevice()
    {
        usbDevice.Close();
        Assert.That(serialPortMock.IsOpen, Is.False);
    }

    [Test, Timeout(2000)]
    public void TestThreadsStop()
    {
        usbDevice.Close();
    }

    [Test]
    public void TestReceiveMessage()
    {
        var (json1, binary1) = GetTestEntry();
        serialPortMock.feed(binary1);
        serialPortMock.feed(Encoding.UTF8.GetBytes("CYNTERACT\n"));
        AssertReceivedMessage(json1);
        Assert.That(messageQueue.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestMultipleMessages()
    {
        var (json1, binary1) = GetTestEntry();
        int count = 1000;
        for (int i = 0; i < count; i++)
        {
            serialPortMock.feed(binary1);
            serialPortMock.feed(Encoding.UTF8.GetBytes("CYNTERACT\n"));
        }
        for (int i = 0; i < count; i++)
        {
            AssertReceivedMessage(json1);
        }
        Assert.That(messageQueue.Count, Is.EqualTo(0));
    }

    [Test]
    public void TestBrokenMessages()
    {
        var (json1, binary1) = GetTestEntry();
        for (int i = 0; i < 5; i++)
        {
            serialPortMock.feed(binary1);
            serialPortMock.feed(Encoding.UTF8.GetBytes("CYNTERACT\n broken stuff CYNTERACT\n"));
        }
        for (int i = 0; i < 5; i++)
        {
            AssertReceivedMessage(json1);
        }
        Assert.That(messageQueue.Count, Is.EqualTo(0));
    }

    private void AssertReceivedMessage(string expected)
    {
        if (!messageQueue.TryTake(out Tuple<string, object>? nameAndMessage, 1000))
            Assert.Fail("No message received.");
        Assert.That(nameAndMessage!.Item1, Is.EqualTo("COM1"));
        string json = Protocol.Serialize("testDevice", nameAndMessage!.Item2);
        TestData.AssertJsonEquals(expected, json);
    }

    private (string, byte[]) GetTestEntry()
    {
        var testEntry = TestData.Data.First(entry => entry["name"] == "debug");
        string json1 = testEntry["protocol"];
        byte[] binary1 = TestData.ParsePrettyPrintedByteArray(testEntry["hardwareProtocol"]);
        return (json1, binary1);
    }

    class SerialPortMock : ISerialPort
    {
        private BlockingCollection<byte> dataStream = [];
        private bool isOpen = false;
        public required string PortName { get; set; }
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public bool IsOpen => isOpen;
        public void Open() { isOpen = true; }
        public void Close() { isOpen = false; }
        public void Write(byte[] bytes, int offset, int count) { }
        public int ReadByte()
        {
            if (!dataStream.TryTake(out byte b, ReadTimeout))
                throw new TimeoutException();
            return b;
        }
        public void DiscardInBuffer() { }
        public void DiscardOutBuffer() { }
        public void feed(byte[] data) => Array.ForEach(data, b => dataStream.Add(b));
    }
}