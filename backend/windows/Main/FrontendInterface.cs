#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace Main
{

    public class Message
    {
        public class Types
        {
            public const string
                Scan = "scan",
                Connect = "connect",
                Disconnect = "disconnect",
                Data = "data",
                Debug = "debug",
                Error = "error"
            ;
        }
        public string Type { get; set; }
        public object Body { get; set; }

        public Message(string Type, object Body)
        {
            this.Type = Type;
            this.Body = Body;
        }


        private readonly static object syncLock = new object();
        public void Write(TextWriter writer)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
            };
            string json = JsonSerializer.Serialize(this, options);
            lock (syncLock)
            {
                writer.WriteLine(json);
            }
            writer.Flush();
        }

        public static Message? ReadLine(TextReader reader)
        {
            string? json = reader.ReadLine();
            if (json == null)
                return null;
            return FromJson(json);
        }

        public static Message FromJson(String json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            Message? message = JsonSerializer.Deserialize<Message>(json, options);
            if (message == null)
                throw new Exception("Failed to deserialize message");
            return message;
        }
    }

}