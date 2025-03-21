using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Connector;
using Connector.Messages;

namespace Test
{
    class LiveTest
    {
        Process? process;
        readonly Stopwatch stopwatch = new();
        // void BluetoothTest()
        // {
        //     Console.WriteLine("First Ble Run...");
        //     Console.WriteLine("Please turn a device on if not already done.");
        //     StartBackend();
        //     Thread.Sleep(5 * 1000);
        //     StopBackend();
        //     Console.WriteLine("Second Ble Run...");
        //     StartBackend();
        //     Thread.Sleep(5 * 1000);
        //     StopBackend();
        // }

        public void UsbTest()
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
                var status = new String[] { "B", "N", "E", "R" }[(int)(data.imuStates![i] & 0b00001111)];
                if (status == "R")
                    Console.Write(String.Format("{0:f}", data.imuValues![i].x).PadLeft(5));
                else
                    Console.Write((new String[] { "B", "N", "E", "R", "C" }
[(int)data.imuValues![i].w]).PadLeft(5));
            }

            // print force
            // Console.ForegroundColor = ConsoleColor.White;
            // var force = data.force;
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
            if (process == null)
                throw new Exception("Process not started");
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
                (string deviceId, object message) = Protocol.Deserialize(args.Data);
                switch (message)
                {
                    case Dataframe dataMessage:
                        OnData(dataMessage);
                        break;
                    case Error errorMessage:
                        Console.WriteLine("----- Error " + deviceId + " -----");
                        Console.WriteLine(errorMessage.message);
                        break;
                    default:
                        Console.WriteLine("----- " + message.GetType().FullName + " -----");
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

    }
}