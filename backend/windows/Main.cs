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
        Program()
        {
            deviceCache = new DeviceCache(usb, bluetooth);
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
                default:
                    return RunMainProgram();
            }
        }
        static int RunMainProgram()
        {
            new Message.Debug { message = "Backend started." }.Write(Console.Out);
            Program program = new Program();
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