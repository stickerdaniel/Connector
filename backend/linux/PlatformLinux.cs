using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace Connector
{
    public static class Platform
    {
        // Returns a list of supported USB ports for the given VID/PID pairs (Linux only)
        public static IEnumerable<string> GetSupportedUsbPorts((string vid, string pid)[] supportedDevices)
        {
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
            // Fallback: if no symlinks found, try all ports
            if (!filteredPorts.Any())
            {
                filteredPorts = new HashSet<string>(SerialPort.GetPortNames());
            }
            return filteredPorts;
        }

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
                    (byIdName.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase) &&
                     byIdName.Contains($"PID_{pid}", StringComparison.OrdinalIgnoreCase))
                    ||
                    (byIdName.Contains($"vid_{vid.ToLower()}", StringComparison.OrdinalIgnoreCase) &&
                     byIdName.Contains($"pid_{pid.ToLower()}", StringComparison.OrdinalIgnoreCase))
                   )
                {
                    return true;
                }
            }
            return false;
        }
    }
}