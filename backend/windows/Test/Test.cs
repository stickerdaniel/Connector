using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Main;

namespace Test
{
    class Program
    {
        string[] devices = new string[0];
        Process process;
        Stopwatch stopwatch = new();
        void BluetoothTest()
        {
            Console.WriteLine("First Ble Run...");
            Console.WriteLine("Please turn a device on if not already done.");
            StartBackend();
            Thread.Sleep(5 * 1000);
            StopBackend();
            Console.WriteLine("Second Ble Run...");
            StartBackend();
            Thread.Sleep(5 * 1000);
            StopBackend();
        }

        void UsbTest()
        {
            Console.WriteLine("First Usb Run...");
            Console.WriteLine("Please plugin a device if not already done.");
            StartBackend();
            Thread.Sleep(5 * 1000);
            StopBackend();
            Console.WriteLine("Second Usb Run...");
            StartBackend();
            Thread.Sleep(5 * 1000);
            StopBackend();
        }

        void OnData(DataReceive dataReceive)
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
                    Console.Write((new String[] { "B", "N", "E", "R", "C" }
[(int)dataReceive.imu[i].w]).PadLeft(5));
            }

            // print force
            Console.ForegroundColor = ConsoleColor.White;
            var force = dataReceive.force;
            //Console.Write("{0} {1} {2} {3} {4} {5} {6} {7}", force[0], force[1], force[2], force[3], force[4], force[5], force[6], force[7]);

            // print elapsed time
            Console.Write("|" + stopwatch.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            stopwatch.Restart();
        }
        void StartBackend()
        {
            process = new();
            process.StartInfo.FileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            // in case of "dotnet run" or the debugger the program is run as dll
            if (process.StartInfo.FileName.EndsWith(".dll"))
                process.StartInfo.FileName = process.StartInfo.FileName.Replace(".dll", ".exe");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            // process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += OnMessage;
            // process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data); // Handle error output

            process.Start();

            process.BeginOutputReadLine();
            // process.BeginErrorReadLine();
        }
        void StopBackend()
        {
            process.Kill();
            process.WaitForExit();
        }
        void OnMessage(object sender, DataReceivedEventArgs args)
        {
            // end of stream reached
            if (args.Data == null)
                return;
            Message message = Message.FromJson(args.Data);
            if (message.Type == Message.Types.Data)
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true,
                };
                DataReceive dataReceive = ((JsonElement)message.Body).GetProperty("data").Deserialize<DataReceive>(options);
                string json = JsonSerializer.Serialize(dataReceive, options);
                OnData(dataReceive);
            }
            else
                Console.WriteLine("Unknown message " + args.Data);
        }
        public static int Main(string[] args)
        {
            DataSend dataSend = new();
            for (int i = 0; i < dataSend.vibration.Length; i++)
                dataSend.vibration[i] = 50;
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            string json = JsonSerializer.Serialize<DataSend>(dataSend, options);
            Console.WriteLine(json);

            Program test = new();
            Console.WriteLine("running tests...");
            // also checkout power/link interruptions
            test.UsbTest();
            // BluetoothTest();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return 0;
        }
    }
}
