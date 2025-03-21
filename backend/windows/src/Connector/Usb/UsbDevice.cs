using System;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Windows.Networking;
using System.IO;
using System.Linq;

namespace Connector
{

    public class UsbDevice
    {
        private ISerialPort serial;
        private CancellationTokenSource readThreadCancellationTokenSource = new();
        private CancellationTokenSource writeThreadCancellationTokenSource = new();
        private Thread readThread, writeThread;
        private readonly object writeLock = new();
        private ConcurrentQueue<object> messageSendQueue = new();
        private class ReadBuffer
        {
            public long transmissionStartTime = 0;
            public byte[] data = new byte[1024];
            public int offset = 0;
        }
        private ReadBuffer readBuffer = new();
        public static readonly byte[] PACKAGE_DELIM = Encoding.UTF8.GetBytes("CYNTERACT\n");
        private HardwareProtocol hardwareProtocol = new();

        public event Action<string> OnRequestConnectionCheck;
        public event Action<string, string> OnDeviceError;
#pragma warning disable 67
        public event Action<string, object> OnDeviceMessage;
#pragma warning restore 67

        public readonly string PortName;



        public UsbDevice(string portName, ISerialPort serialPort)
        {
            PortName = portName;
            serial = serialPort;
        }

        public int BufferSize
        {
            get { return readBuffer.data.Length; }
        }
        public int PackageTimeout
        {
            get { return 2000; }
        }

        public void Start()
        {
            serial.Open();
            serial.DiscardOutBuffer();
            serial.DiscardInBuffer();


            readThread = new Thread(() => ReadRoutine(readThreadCancellationTokenSource.Token));
            readThread.Start();
            writeThread = new Thread(() => WriteRoutine(writeThreadCancellationTokenSource.Token));
            writeThread.Start();

        }
        public void Close()
        {
            try
            {
                readThreadCancellationTokenSource.Cancel();
                writeThreadCancellationTokenSource.Cancel();
                readThread.Join();
                writeThread.Join();
                serial.Close();
            }
            catch (Exception e)
            {
                OnDeviceError?.Invoke(PortName, e.Message);
            }

        }

        void ReceiveMessage(CancellationToken cancellationToken)
        {
            int x;
            while ((x = serial.ReadByte()) != -1 && !cancellationToken.IsCancellationRequested)
            {
                // case 1: last package timed out
                if (readBuffer.transmissionStartTime > 0 && DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond > readBuffer.transmissionStartTime + PackageTimeout)
                {
                    OnDeviceError?.Invoke(PortName, "Package timeout.");
                    // assume the start of a new package
                    readBuffer.transmissionStartTime = 0;
                }
                // case 2: transmission of new package
                if (readBuffer.transmissionStartTime == 0)
                {
                    readBuffer.offset = 0;
                    readBuffer.transmissionStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }
                // case 3: buffer overflow, this package will be corrupted
                if (readBuffer.offset == readBuffer.data.Length)
                {
                    OnDeviceError?.Invoke(PortName, "ReadBuffer overflow.");
                    // continue reading the package until the end, event if it is corrupted
                    readBuffer.offset = 0;
                }
                // case 4: continuation of already started package
                readBuffer.data[readBuffer.offset++] = (byte)x;
                // case 5: end of package reached
                if (readBuffer.offset >= PACKAGE_DELIM.Length &&
                    readBuffer.data.AsSpan(readBuffer.offset - PACKAGE_DELIM.Length, PACKAGE_DELIM.Length).SequenceEqual(PACKAGE_DELIM))
                {
                    readBuffer.transmissionStartTime = 0;
                    BinaryReader binaryReader = new(new MemoryStream(readBuffer.data, 0, readBuffer.offset - PACKAGE_DELIM.Length));
                    object message = hardwareProtocol.Deserialize(binaryReader);
                    OnDeviceMessage.Invoke(PortName, message);
                }
            }
        }

        public void SendMessage(object message)
        {
            lock (writeLock)
            {
                using (MemoryStream stream = new())
                {

                    using (BinaryWriter writer = new(stream))
                    {
                        hardwareProtocol.Serialize(writer, message);

                        serial.Write(stream.GetBuffer(), 0, (int)stream.Length);
                        serial.Write(PACKAGE_DELIM, 0, PACKAGE_DELIM.Length);
                    }
                }
            }
        }

        void ReadRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReceiveMessage(cancellationToken);
                }
                catch (Exception e)
                {
                    OnDeviceError?.Invoke(PortName, e.ToString());
                    Thread.Sleep(50);
                    // check if device is still connected as com device
                    OnRequestConnectionCheck?.Invoke(PortName);

                    // stream was closed or has problems, wait for reconnection/resolution
                    if (e is not TimeoutException)
                        Thread.Sleep(500);
                }

            }
        }
        void WriteRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (!messageSendQueue.IsEmpty)
                    {
                        if (messageSendQueue.TryDequeue(out object message))
                        {
                            SendMessage(message);
                        }
                        Thread.Sleep(50);
                    }
                }

                catch (Exception e)
                {
                    OnDeviceError?.Invoke(PortName, e.ToString());
                    Thread.Sleep(50);
                    // check if device is still connected as com device

                    OnRequestConnectionCheck?.Invoke(PortName);
                    // stream was closed or has problems, wait for reconnection/resolution
                    if (e is not TimeoutException)
                        Thread.Sleep(500);
                }
            }
        }
    }
}
