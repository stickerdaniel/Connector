using System;
using System.Collections.Generic;
using System.IO.Ports;
using Microsoft.Win32;

namespace Connector
{
    public static class Platform
    {
        public static IEnumerable<string> GetSupportedUsbPorts((string vid, string pid)[] supportedDevices)
        {
            Console.WriteLine("[Platform.cs] Using Windows platform code");
            var ports = new HashSet<string>();
            using (var serialKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            using (var usbRootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
            {
                foreach (var portName in ListValues(serialKey))
                {
                    if (string.IsNullOrEmpty(portName))
                        continue;
                    foreach (var deviceKeyName in ListSubKeys(usbRootKey))
                    {
                        if (IsSupportedDevice(deviceKeyName, supportedDevices))
                        {
                            using (var deviceKey = usbRootKey.OpenSubKey(deviceKeyName))
                            {
                                foreach (var instanceKeyName in ListSubKeys(deviceKey))
                                {
                                    using (var deviceParameters = deviceKey.OpenSubKey($"{instanceKeyName}\\Device Parameters"))
                                    {
                                        string port = deviceParameters?.GetValue("PortName") as string;
                                        if (string.Equals(port, portName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            ports.Add(portName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ports;
        }

        private static IEnumerable<string> ListSubKeys(RegistryKey key)
        {
            if (key != null)
                foreach (var subKeyName in key.GetSubKeyNames())
                    yield return subKeyName;
        }

        private static IEnumerable<string> ListValues(RegistryKey key)
        {
            if (key != null)
                foreach (var valueName in key.GetValueNames())
                    yield return key.GetValue(valueName)?.ToString();
        }

        private static bool IsSupportedDevice(string deviceKeyName, (string vid, string pid)[] supportedDevices)
        {
            foreach (var (vid, pid) in supportedDevices)
            {
                if ((deviceKeyName.Contains($"VID_{vid}") && deviceKeyName.Contains($"PID_{pid}")))
                {
                    return true;
                }
            }
            return false;
        }
    }
}