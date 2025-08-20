using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Connector
{
    // Windows-only version extracted from oldPlatform.cs (Linux code removed)
    public static class Platform
    {
        // Returns a list of supported USB (COM) ports for the given VID/PID pairs
        public static IEnumerable<string> GetSupportedUsbPorts((string vid, string pid)[] supportedDevices)
        {
            Console.WriteLine("[Platform.windows.cs] Using Windows platform code");
            var ports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (var serialKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            using (var usbRootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
            {
                foreach (var portName in ListValues(serialKey))
                {
                    if (string.IsNullOrEmpty(portName))
                        continue;

                    foreach (var deviceKeyName in ListSubKeys(usbRootKey))
                    {
                        if (!IsSupportedDevice(deviceKeyName, supportedDevices))
                            continue;

                        using (var deviceKey = usbRootKey.OpenSubKey(deviceKeyName))
                        {
                            foreach (var instanceKeyName in ListSubKeys(deviceKey))
                            {
                                using (var deviceParameters = deviceKey.OpenSubKey($"{instanceKeyName}\\Device Parameters"))
                                {
                                    string port = deviceParameters?.GetValue("PortName") as string;
                                    if (string.Equals(port, portName, StringComparison.OrdinalIgnoreCase))
                                        ports.Add(portName);
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
            if (key == null) yield break;
            foreach (var n in key.GetSubKeyNames()) yield return n;
        }

        private static IEnumerable<string> ListValues(RegistryKey key)
        {
            if (key == null) yield break;
            foreach (var v in key.GetValueNames()) yield return key.GetValue(v)?.ToString();
        }

        private static bool IsSupportedDevice(string deviceKeyName, (string vid, string pid)[] supportedDevices)
        {
            foreach (var (vid, pid) in supportedDevices)
            {
                if (deviceKeyName.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase) &&
                    deviceKeyName.Contains($"PID_{pid}", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}