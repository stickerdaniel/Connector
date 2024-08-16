using System;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Windows.Networking;

namespace Connector
{

    class UsbDevice
    {
        public event Action<string> OnRequestConnectionCheck;
        public event Action<string, string> OnDeviceError;
        public event Action<string, InformationV1In> OnDeviceInformation;
        public event Action<string, DataReceive> OnDeviceData;
        public event Action<string, string> OnDeviceDebug;
        public readonly string PortName;

        private SerialPort serial;
        private Thread readThread, writeThread;

        private CancellationTokenSource readThreadCancellationTokenSource = new();
        private CancellationTokenSource writeThreadCancellationTokenSource = new();
        private readonly PackageReadBuffer packageReadBuffer = new();
        private readonly byte[] packageSendBuffer = new byte[Protocol.DATA_SEND_SIZE];
        private readonly object writeLock = new();
        private ConcurrentQueue<DataSend> dataSendQueue = new();

        public UsbDevice(string portName)
        {
            PortName = portName;
            serial = new SerialPort(portName, 230400)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
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

                serial.Close();
                readThreadCancellationTokenSource.Cancel();
                readThread.Join();
                writeThreadCancellationTokenSource.Cancel();
                writeThread.Join();
            }
            catch (Exception e)
            {
                OnDeviceError?.Invoke(PortName, e.Message);
            }

        }
        void WriteRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (!dataSendQueue.IsEmpty)
                    {
                        if (dataSendQueue.TryDequeue(out DataSend dataSend))
                        {
                            Console.WriteLine("Sending data");
                            SendData(dataSend);
                        }
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
        void ReadRoutine(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ReadPackage();
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
        void ReadPackage()
        {
            int x;
            while ((x = serial.ReadByte()) != -1)
            {
                // case 1: last package timed out
                if (packageReadBuffer.transmissionStartTime > 0 && DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond > packageReadBuffer.transmissionStartTime + 2000)
                {
                    Console.Error.WriteLine("package timed out");
                    // assume the start of a new package
                    packageReadBuffer.transmissionStartTime = 0;
                }
                // case 2: transmission of new package
                if (packageReadBuffer.transmissionStartTime == 0)
                {
                    packageReadBuffer.offset = 0;
                    packageReadBuffer.transmissionStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                }
                // case 3: buffer overflow, this package will be corrupted
                if (packageReadBuffer.offset == packageReadBuffer.data.Length)
                {
                    Console.Error.WriteLine("buffer overflow");
                    // continue reading the package until the end, event if it is corrupted
                    packageReadBuffer.offset = 0;
                }
                // case 4: continuation of already started package
                packageReadBuffer.data[packageReadBuffer.offset++] = (byte)x;
                // case 5: end of package reached
                if (packageReadBuffer.offset >= Protocol.PACKAGE_DELIM.Length &&
                     Protocol.MemoryCompare(Protocol.PACKAGE_DELIM, packageReadBuffer.data, packageReadBuffer.offset - Protocol.PACKAGE_DELIM.Length))
                {
                    //Console.Error.WriteLine(b.offset);
                    packageReadBuffer.transmissionStartTime = 0;
                    if (Protocol.MemoryCompare(Encoding.ASCII.GetBytes("DATA"), packageReadBuffer.data))
                    {
                        if (packageReadBuffer.offset - Protocol.PACKAGE_DELIM.Length != Protocol.DATA_RECEIVE_SIZE)
                        {
                            Console.Error.WriteLine("data packet has wrong size");
                        }
                        else
                        {
                            DataReceive dataReceive = Protocol.DeserializeData<DataReceive>(packageReadBuffer.data);
                            OnDeviceData?.Invoke(PortName, dataReceive);
                        }
                    }
                    else if (Protocol.MemoryCompare(Encoding.ASCII.GetBytes("DEBUG"), packageReadBuffer.data))
                    {
                        string debugReceive = Encoding.ASCII.GetString(packageReadBuffer.data, 5, packageReadBuffer.offset - 5 - Protocol.PACKAGE_DELIM.Length);
                        OnDeviceDebug?.Invoke(PortName, debugReceive);
                    }
                    else if (packageReadBuffer.data[0] == '{')
                    {
                        // glove information sent as modified json without quotes
                        string information = Encoding.ASCII.GetString(packageReadBuffer.data, 0, packageReadBuffer.offset - Protocol.PACKAGE_DELIM.Length);
                        // unstrip double quotes to create valid json again
                        string json = Regex.Replace(information, @"[\w]+", (m) => '"' + m.ToString() + '"');
                        InformationV1In deserialized = JsonHelper.FromJson<InformationV1In>(json);
                        OnDeviceInformation?.Invoke(PortName, deserialized);
                    }

                }
            }

        }
        public void RequestInformation()
        {
            DataSend data = new()
            {
                requestInformation = true
            };
            dataSendQueue.Enqueue(data);
        }
        public void SendData(DataSend data)
        {
            lock (writeLock)
            {
                Protocol.SerializeData<DataSend>(data, packageSendBuffer);
                serial.Write(packageSendBuffer, 0, packageSendBuffer.Length);
                serial.Write(Protocol.PACKAGE_DELIM, 0, Protocol.PACKAGE_DELIM.Length);
            }
        }


    }
}
