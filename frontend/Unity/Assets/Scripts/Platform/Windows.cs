
// System.Diagnostic does not work in Windows export, only in editor
// TODO: implement il2cpp compatible version of Process

#if UNITY_EDITOR

using Main;
using System;
using System.Diagnostics;
using UnityEngine;
class PlatformSpecific
{
    Process? process;

    public void Start(DevicesImpl devicesImpl)
    {
        process = new Process();
        process.StartInfo.FileName = Application.dataPath + "./Plugins/Windows/Connector.exe";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        // keep terminal window open in case Unity doesn't stop the process
        process.StartInfo.CreateNoWindow = false;

        // process.OutputDataReceived += OnMessage;
        process.OutputDataReceived += (sender, args) =>
        {
            // end of stream reached, backend has stopped
            if (args.Data == null)
                return;

            devicesImpl.OnMessage(args.Data);
        };
        process.ErrorDataReceived += (sender, args) => devicesImpl.RaiseError(new Exception(args.Data));

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

    public void SendMessage(Message message)
    {
        if (process == null || process.HasExited)
            throw new Exception("Backend is not running");

        message.Write(process.StandardInput);
    }
}

#endif