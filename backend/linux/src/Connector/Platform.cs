using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
#if WINDOWS
using System.Management;
#endif

namespace Connector
{
    public static class Platform
    {
        // Returns a list of supported USB ports for the given VID/PID pairs
        public static IEnumerable<string> GetSupportedUsbPorts((string vid, string pid)[] supportedDevices)
        {
#if WINDOWS
            Console.WriteLine("[Platform.cs] Using Windows platform code");
            // Windows: Use registry and WMI to filter COM ports by VID/PID
            var ports = new HashSet<string>();
            using (var serialKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            using (var usbRootKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
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
#else
            Console.WriteLine("[Platform.cs] Using Linux platform code");
            // Linux: Use /dev/serial/by-id/ symlinks and filter by VID/PID
            var map = new Dictionary<string, string>();
            string byIdDir = "/dev/serial/by-id/";
            if (Directory.Exists(byIdDir))
            {
                foreach (var symlink in Directory.GetFiles(byIdDir))
                {
                    try
                    {
                        var target = GetRealDevice(symlink);
                        map[target] = Path.GetFileName(symlink);
                    }
                    catch { }
                }
            }
            var filteredPorts = new HashSet<string>();
            foreach (var kvp in map)
            {
                if (IsSupportedDevice(kvp.Value, supportedDevices))
                {
                    filteredPorts.Add(kvp.Key);
                }
            }
            // Fallback: if no symlinks found, try all ports (may connect too much)
            if (!filteredPorts.Any())
            {
                filteredPorts = new HashSet<string>(SerialPort.GetPortNames());
            }
            return filteredPorts;
#endif
        }
#if WINDOWS
        private static IEnumerable<string> ListSubKeys(Microsoft.Win32.RegistryKey key)
        {
            if (key != null)
                foreach (var subKeyName in key.GetSubKeyNames())
                    yield return subKeyName;
        }
        private static IEnumerable<string> ListValues(Microsoft.Win32.RegistryKey key)
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
#else
        private static string GetRealDevice(string symlink)
        {
            try
            {
                var fi = new FileInfo(symlink);
                if (!string.IsNullOrEmpty(fi.LinkTarget))
                {
                    if (!Path.IsPathRooted(fi.LinkTarget))
                    {
                        string byIdDir = "/dev/serial/by-id/";
                        string full = Path.GetFullPath(Path.Combine(byIdDir, fi.LinkTarget));
                        return File.Exists(full) ? full : symlink;
                    }
                    return File.Exists(fi.LinkTarget) ? fi.LinkTarget : symlink;
                }
            }
            catch { }
            return symlink;
        }
        private static bool IsSupportedDevice(string byIdName, (string vid, string pid)[] supportedDevices)
        {
            foreach (var (vid, pid) in supportedDevices)
            {
                if (
                    byIdName.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase) && byIdName.Contains($"PID_{pid}", StringComparison.OrdinalIgnoreCase)
                    || byIdName.Contains($"vid_{vid.ToLower()}", StringComparison.OrdinalIgnoreCase) && byIdName.Contains($"pid_{pid.ToLower()}", StringComparison.OrdinalIgnoreCase)
                )
                {
                    return true;
                }
            }
            return false;
        }
#endif
    }
}