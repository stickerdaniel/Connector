using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
#nullable enable
namespace Connector
{
    class MacOSPlatformSpecific : IPlatformSpecific
    {
        Process? process;

        public void Start(DeviceManager deviceManager)
        {
            process = new Process();
            process.StartInfo.FileName = GetConnectorExeFilePath();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (sender, args) =>
            {
                // end of stream reached, backend has stopped
                if (args.Data == null)
                    return;

                deviceManager.OnMessage(args.Data);
            };
            process.ErrorDataReceived += (sender, args) => deviceManager.RaiseError(new Exception(args.Data));

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
                process = null;
            }
        }

        public void SendMessage(string serializedMessage)
        {
            if (process == null || process.HasExited)
                throw new Exception("Backend is not running");

            process.StandardInput.WriteLine(serializedMessage);
        }

        public string? GetConnectorExeFilePath()
        {
            var files = Directory.GetFiles(Application.streamingAssetsPath);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                // Look for macOS binary (no extension, or .app bundle)
                if (Regex.IsMatch(fileName, @"Connector\.MacOS") && !fileName.Contains(".meta"))
                {
                    return file;
                }
            }
            return null;
        }
    }
}
