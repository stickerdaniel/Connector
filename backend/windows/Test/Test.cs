using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Connector;

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

        void OnData(Dataframe data)
        {
            // print x-angles
            for (int i = 0; i < 16; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(("|" + i).PadLeft(2));
                Console.ForegroundColor = ConsoleColor.White;
                // error code
                var status = new String[] { "B", "N", "E", "R" }[(int)(data.imuStatus[i] & 0b00001111)];
                if (status == "R")
                    Console.Write(String.Format("{0:f}", data.imu[i].x).PadLeft(5));
                else
                    Console.Write((new String[] { "B", "N", "E", "R", "C" }
[(int)data.imu[i].w]).PadLeft(5));
            }

            // print force
            Console.ForegroundColor = ConsoleColor.White;
            var force = data.force;
            //Console.Write("{0} {1} {2} {3} {4} {5} {6} {7}", force[0], force[1], force[2], force[3], force[4], force[5], force[6], force[7]);

            // print elapsed time
            Console.Write("|" + stopwatch.ElapsedMilliseconds + "ms");

            Console.WriteLine();

            stopwatch.Restart();
        }
        void StartBackend()
        {
            process = new();
            process.StartInfo.FileName = System.AppContext.BaseDirectory + "Connector.exe";
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
            try
            {
                Message message = Message.FromJson(args.Data);
                switch (message)
                {
                    case Message.Data dataMessage:
                        OnData(dataMessage.data);
                        break;
                    case Message.Error errorMessage:
                        Console.WriteLine("----- " + errorMessage.type + " " + errorMessage.deviceId + " -----");
                        Console.WriteLine(errorMessage.message);
                        break;
                    default:
                        Console.WriteLine("----- " + message.type + " -----");
                        var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                        string prettyJson = JsonSerializer.Serialize<object>(message, options);
                        Console.WriteLine(prettyJson);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
        public static int Main(string[] args)
        {
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
