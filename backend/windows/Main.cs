#nullable enable

using System;
using System.IO;

namespace Connector
{
    class Program
    {
        readonly Usb usb = new();
        readonly Ble bluetooth = new();
        readonly DeviceCache deviceCache;
        Program(string jsonPath)
        {
            deviceCache = new DeviceCache(usb, bluetooth, jsonPath);
            deviceCache.LoadInformationJson();
            deviceCache.OnMessageOut += (message) => message.Write(Console.Out);
            usb.Init();
        }
        bool ProcessInput()
        {
            Message message = Message.ReadLine(Console.In)!;
            deviceCache.OnMessageIn(message);
            return false;
        }

        static int Main(string[] args)
        {
            switch (args)
            {
                case ["test"]:
                    return Test.Program.Main(args);
                case ["jsonPath", var path]:
                    return RunMainProgram(path);
                default:
                    return RunMainProgram(
                        Path.Combine( 
                            AppDomain.CurrentDomain.BaseDirectory,
                            "StandardDeviceInformation.json"
                            )
                        );
            }
        }
        static int RunMainProgram(string jsonPath)
        {
            new Message.Debug { message = "Backend started." }.Write(Console.Out);
            Program program = new Program(jsonPath);
            bool exit = false;
            while (!exit)
            {
                try
                {
                    exit = program.ProcessInput();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    Console.Error.Flush();
                }
            }
            return 0;
        }
    }
}