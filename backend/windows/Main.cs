using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
//using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Cynteract.CGlove;
using Microsoft.Win32;

namespace Test02_NETFramework
{
    class Program
    {
        public static void BluetoothTest()
        {
            // check multiple runs, similar to Unity's start/stop scene cycle
            Console.WriteLine("First Run...");
            GloveCommunication comm = StartCommunication(
                BLE.ScanDevices,
                (deviceId) =>
                    {
                        Glove glove = new Glove();
                        glove.bleId = deviceId;
                        return new BLE(glove);
                    }
                );
            Thread.Sleep(8 * 1000);
            comm?.Close();
            Thread.Sleep(2000);

            Console.WriteLine("Second Run...");
            comm = StartCommunication(
                BLE.ScanDevices,
                (deviceId) =>
                {
                    Glove glove = new Glove();
                    glove.bleId = deviceId;
                    return new BLE(glove);
                }
                );
            Thread.Sleep(8 * 1000);
            comm?.Close();
            Thread.Sleep(2000);
        }

        public static void SerialTest()
        {
            Console.WriteLine("First Serial Run...");
            GloveCommunication comm = StartCommunication(
                USB.ScanDevices,
                (deviceId) =>
                {
                    Glove glove = new Glove();
                    glove.comPort = deviceId;
                    return new USB(glove);
                }
                );
            Thread.Sleep(5 * 1000);
            comm?.Close();
            Thread.Sleep(1000);

            Console.WriteLine("Second Serial Run...");
            comm = StartCommunication(
                USB.ScanDevices,
                (deviceId) =>
                {
                    Glove glove = new Glove();
                    glove.comPort = deviceId;
                    return new USB(glove);
                }
                );
            Thread.Sleep(5 * 1000);
            comm?.Close();
            Thread.Sleep(1000);
        }

        public static GloveCommunication StartCommunication(
            Func<GloveCommunication.Scan> StartScan,
            Func<string, GloveCommunication> Create
            )
        {
            string deviceId = null;
            GloveCommunication.Scan scan = StartScan();
            scan.Found = (_deviceId) =>
            {
                Console.WriteLine("found device");
                if (deviceId == null)
                    deviceId = _deviceId;
            };
            scan.Finished = () =>
            {
                Console.WriteLine("scan finished");
                if (deviceId == null)
                    deviceId = "-1";
            };
            while (deviceId == null)
                Thread.Sleep(500);
            scan.Cancel();
            if (deviceId == "-1")
            {
                Console.WriteLine("no glove found!");
                return null;
            }
            var stopwatch = new Stopwatch();
            GloveCommunication comm = Create(deviceId);
            comm.onDataChanged = (dataReceive) =>
            {
                // print x-angles
                for (int i = 0; i < 16; i++)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(("|" + i).PadLeft(2));
                    Console.ForegroundColor = ConsoleColor.White;
                    // error code
                    var status = new String[] { "B", "N", "E", "R" }[(int)(dataReceive.imuStatus[i] & 0b00001111)];
                    if (status == "R")
                        Console.Write(String.Format("{0:f}", dataReceive.imu[i].x).PadLeft(5));
                    else
                        Console.Write((new String[] { "B", "N", "E", "R", "C" }[(int)dataReceive.imu[i].w]).PadLeft(5));
                }

                // print force
                Console.ForegroundColor = ConsoleColor.White;
                var force = dataReceive.force;
                //Console.Write("{0} {1} {2} {3} {4} {5} {6} {7}", force[0], force[1], force[2], force[3], force[4], force[5], force[6], force[7]);

                // print elapsed time
                Console.Write("|" + stopwatch.ElapsedMilliseconds + "ms");

                Console.WriteLine();

                stopwatch.Restart();
            };

            comm.Start();

            Thread.Sleep(1000);
            comm.RetrieveGloveInformation((message) =>
            {
                var json = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(message), new XmlDictionaryReaderQuotas());
                XElement root = XElement.Load(json);
                Console.WriteLine(message);
                if (int.Parse(root.XPathSelectElement("//SizeOfDataSend").Value) != Marshal.SizeOf(comm.dataReceive)
                    || int.Parse(root.XPathSelectElement("//SizeOfDataReceive").Value) != Marshal.SizeOf(comm.dataSend))
                {
                    throw new ApplicationException("Protocol mismatch, data packages have wrong size!");
                }
            });

            return comm;
        }

        static int Main(string[] args)
        {
            // also checkout power/link interruptions
            // SerialTest();
            BluetoothTest();
            Console.ReadLine();
            return 0;
        }
        /*
            for (int i = 0; i < 10; i++)
            {
                // protocol.dataSend.vibration[i] = (byte)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 10 % 101);
            }
            for (int i = 0; i < 10; i++)
                protocol.dataSend.vibrationPattern[i] = 1;
            protocol.SendData();
            Thread.Sleep(20);
        }*/
    }
}
