using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;

namespace Connector
{
    public static class Platform
    {
        // Returns a list of supported USB ports for the given VID/PID pairs (macOS)
        public static IEnumerable<string> GetSupportedUsbPorts((string vid, string pid)[] supportedDevices)
        {
            var filteredPorts = new HashSet<string>();

            try
            {
                // Run ioreg to get USB device information with serial port mappings
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/ioreg",
                    Arguments = "-r -c IOUSBHostDevice -l",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return FallbackGetPorts();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Parse ioreg output to find devices with matching VID/PID and their IOCalloutDevice
                var devices = ParseIoregOutput(output, supportedDevices);
                foreach (var device in devices)
                {
                    filteredPorts.Add(device);
                }
            }
            catch
            {
                // If ioreg parsing fails, fall back to listing all ports
            }

            // Fallback: if no devices found via ioreg, try all serial ports
            if (!filteredPorts.Any())
            {
                return FallbackGetPorts();
            }

            return filteredPorts;
        }

        private static IEnumerable<string> FallbackGetPorts()
        {
            return new HashSet<string>(SerialPort.GetPortNames());
        }

        private static IEnumerable<string> ParseIoregOutput(string output, (string vid, string pid)[] supportedDevices)
        {
            var results = new List<string>();

            // Convert hex VID/PID to decimal for matching (ioreg outputs decimal values)
            var decimalDevices = new List<(int vid, int pid)>();
            foreach (var (vid, pid) in supportedDevices)
            {
                if (int.TryParse(vid, System.Globalization.NumberStyles.HexNumber, null, out int vidDec) &&
                    int.TryParse(pid, System.Globalization.NumberStyles.HexNumber, null, out int pidDec))
                {
                    decimalDevices.Add((vidDec, pidDec));
                }
            }

            // Split output into top-level device blocks (+-o at start of line or with minimal indentation)
            var deviceBlocks = Regex.Split(output, @"(?=^\+-o\s)", RegexOptions.Multiline);

            foreach (var block in deviceBlocks)
            {
                if (string.IsNullOrWhiteSpace(block))
                    continue;

                // Extract VID and PID from this block (ioreg outputs decimal values)
                var vidMatch = Regex.Match(block, "\"idVendor\"\\s*=\\s*(\\d+)");
                var pidMatch = Regex.Match(block, "\"idProduct\"\\s*=\\s*(\\d+)");

                if (!vidMatch.Success || !pidMatch.Success)
                    continue;

                if (!int.TryParse(vidMatch.Groups[1].Value, out int blockVid) ||
                    !int.TryParse(pidMatch.Groups[1].Value, out int blockPid))
                    continue;

                // Check if this device matches any supported VID/PID
                bool isSupported = decimalDevices.Any(d => d.vid == blockVid && d.pid == blockPid);

                if (isSupported)
                {
                    // Look for IOCalloutDevice in this block (nested in child nodes)
                    // Pattern: "IOCalloutDevice" = "/dev/cu.usbmodemXXX"
                    var calloutMatch = Regex.Match(block, "\"IOCalloutDevice\"\\s*=\\s*\"([^\"]+)\"");
                    if (calloutMatch.Success)
                    {
                        results.Add(calloutMatch.Groups[1].Value);
                    }
                }
            }

            return results;
        }

        // Stub for USB monitoring (not implemented for macOS, like Linux)
        public static void StartUsbMonitoring(Action onChanged)
        {
            // Not implemented - could use FSEvents or polling in future
        }
    }
}
