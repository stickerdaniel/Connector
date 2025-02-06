using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#nullable enable
namespace Connector
{
    public class StandardDeviceInformationManager
    {
        public readonly string FilePath;
        private object fileLock = new();
        public StandardDeviceInformationManager(string filePath)
        {
            FilePath = filePath;
        }
        public void Init()
        {
            lock (fileLock)
            {
                if (!File.Exists(FilePath))
                {
                    SaveInformation(new StandardDeviceInformation());
                }
                var info=LoadInformation();
            }
        }
        public StandardDeviceInformation? LoadInformation()
        {
            lock (fileLock)
            {
                if (!File.Exists(FilePath))
                {
                    return null;
                }
                string jsonString = File.ReadAllText(FilePath);
                return FromJson(jsonString);
            }

        }
        public void SaveInformation(StandardDeviceInformation information)
        {
            lock (fileLock)
            {
                var informationJson = ToJson(information);
                File.WriteAllText(FilePath, informationJson);
            }
        }
        public Information? LoadInformation(string key)
        {
            var deviceInfo = LoadInformation();
            if (deviceInfo == null)
            {
                return null;
            }
            if (deviceInfo.DeviceInformation.ContainsKey(key))
            {
                return deviceInfo.DeviceInformation[key];
            }
            return null;
        }
        public void UpdateInformation(string key,Information information)
        {
            lock (fileLock)
            {
                if (!File.Exists(FilePath))
                {
                    throw new FileNotFoundException("Information Json not found. Call Init() first");
                }
                string jsonString = File.ReadAllText(FilePath);
                var standardInfo=FromJson(jsonString);
                
                standardInfo!.UpdateInformation(key, information);
                SaveInformation(standardInfo);
            }
        }
        public StandardDeviceInformation FromJson(string jsonString)
        {
            return JsonHelper.FromJson<SerializableStandardDeviceInformation>(jsonString).ToStandardDeviceInformation();
        }
        public string ToJson(StandardDeviceInformation information)
        {
            return JsonHelper.ToJson(new SerializableStandardDeviceInformation(information));
        }
    }
    public class StandardDeviceInformation
    {
        public Dictionary<string, Information> DeviceInformation;

        public StandardDeviceInformation()
        {
            DeviceInformation = new();
        }

        public StandardDeviceInformation(Dictionary<string, Information> deviceInformation)
        {
            DeviceInformation = deviceInformation;
        }

        public void UpdateInformation(string key, Information information)
        {
            if (DeviceInformation.ContainsKey(key))
            {
                DeviceInformation[key]=information;
            }
            else
            {
                DeviceInformation.Add(key, information);
            }
        }
    }
    #region Serialization Helper classes
    [Serializable]
    public class SerializableStandardDeviceInformation
    {
        public DictionarySerializable<string,Information> DeviceInformation;
        public SerializableStandardDeviceInformation(StandardDeviceInformation standardDeviceInformation)
        {
            DeviceInformation = new DictionarySerializable<string, Information>(standardDeviceInformation.DeviceInformation);
        }
        public StandardDeviceInformation ToStandardDeviceInformation()
        {
            return new StandardDeviceInformation(DeviceInformation.ToDictionary());
        }
    }
    [Serializable]
    public class KeyValuePairSerializable<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyValuePairSerializable(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
    [Serializable]
    public class DictionarySerializable<TKey, TValue>
    {
        public List<KeyValuePairSerializable<TKey, TValue>> KeyValuePairs = new List<KeyValuePairSerializable<TKey, TValue>>();

        public DictionarySerializable(Dictionary<TKey, TValue> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                KeyValuePairs.Add(new KeyValuePairSerializable<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();
            foreach (var kvp in KeyValuePairs)
            {
                dictionary[kvp.Key] = kvp.Value;
            }
            return dictionary;
        }
    }
    #endregion
}