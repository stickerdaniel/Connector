using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Connector.Messages
{
    public static class Factory
    {
        static Factory()
        {
            // Trigger the static constructors of all message classes
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();
            var targetTypes = types.Where(t => t.IsClass && t.Namespace != null && t.Namespace.StartsWith("Connector.Messages"));
            foreach (var type in targetTypes)
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }
        }
        static Dictionary<string, Type> messageTypes = new();
        public static void RegisterMessageType(string type, Type messageType)
        {
            if (messageTypes.ContainsKey(type))
            {
                throw new Exception("Message type already registered: " + type);
            }
            messageTypes[type] = messageType;
        }
        public static string GetMessageTypeName(object message)
        {
            foreach (var entry in messageTypes)
            {
                if (entry.Value == message.GetType())
                {
                    return entry.Key;
                }
            }
            throw new Exception($"Unknown message type: {message.GetType().FullName}");
        }
        public static Type GetMessageType(string type)
        {
            if (!messageTypes.ContainsKey(type))
            {
                throw new Exception("Unknown message type: " + type);
            }
            return messageTypes[type];
        }
    }
}