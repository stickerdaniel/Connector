#nullable enable

using System;
using System.IO;
using System.Linq;
using Connector.Messages;

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
            deviceCache.OnMessage += (deviceId, message) => Console.Out.WriteLine(Protocol.Serialize(deviceId, message));
            usb.Init();
        }
        bool ProcessInput()
        {
            string? line = Console.In.ReadLine();
            if (line == null)
                return true;
            (string deviceId, object message) = Protocol.Deserialize(line);
            deviceCache.SendMessage(deviceId, message);
            return false;
        }

        static int Main(string[] args)
        {
            Console.WriteLine("Version 3.0.0");
            Console.Out.WriteLine(
                Protocol.Serialize(
                    null,
                    new Debug { message = "Backend started." }
                )
            );
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