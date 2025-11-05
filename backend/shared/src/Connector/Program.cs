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
            // Confirm correct parsing to frontend
            string messageEcho = Protocol.Serialize(deviceId, message);
            Console.Out.WriteLine(
                Protocol.Serialize(
                    null,
                    new Debug { message = $"Parsed message in backend: {messageEcho}" }
                )
            );
            deviceCache.SendMessage(deviceId, message);
            return false;
        }

        static int Main(string[] args)
        {
            Console.Out.WriteLine(
                Protocol.Serialize(
                    null,
                    new Debug { message = "Backend started." }
                )
            );

            Program program;
            try
            {
                program = new Program();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(
                    Protocol.Serialize(
                        null,
                        new Error { message = e.Message }
                    )
                );
                return 1;
            }

            bool exit = false;
            while (!exit)
            {
                try
                {
                    exit = program.ProcessInput();
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(
                        Protocol.Serialize(
                            null,
                            new Error { message = e.Message }
                        )
                    );
                }
            }
            return 0;
        }
    }
}