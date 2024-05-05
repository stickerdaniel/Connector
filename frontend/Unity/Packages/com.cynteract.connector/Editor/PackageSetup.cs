
using System.IO;
using System.Diagnostics;
using System.Text;
using System;

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PackageSetup
{
    static string downloadPath = "Assets/Connector";

    static PackageSetup()
    {
        EditorApplication.delayCall += DownloadArtifacts;
    }

    public static string GetPackageVersion()
    {
        string packageName = "com.cynteract.connector";

        // Retrieve package information from Package Manager
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{packageName}");

        if (packageInfo != null)
        {
            // Return the version of the package
            return packageInfo.version;
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Package '{packageName}' not found.");
            return null;
        }
    }

    private static string ExecuteCommand(string commandName, string arguments)
    {
        StringBuilder outputBuilder = new StringBuilder();
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(commandName);
            startInfo.Arguments = arguments;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            outputBuilder.AppendLine($"Error running process: {ex.Message}");
        }

        return outputBuilder.ToString();
    }

    private static void DownloadArtifacts()
    {
        string packageVersion = GetPackageVersion();

        if (!AssetDatabase.IsValidFolder(downloadPath))
        {
            UnityEngine.Debug.Log("Creating Connector folder...");
            AssetDatabase.CreateFolder("Assets", "Connector");
        }
        string windowsPath = $"{downloadPath}/Connector.exe";
        if (!File.Exists(windowsPath))
        {
            UnityEngine.Debug.Log($"Downloading Connector.exe, version {packageVersion} for Windows...");
            // Download via gh-cli. You need to run `gh auth login` first.
            string command = $"release download v{packageVersion} --repo Cynteract/Connector --pattern \"*.exe\" --dir {downloadPath}";
            string output = ExecuteCommand("gh", command);
            UnityEngine.Debug.Log(output);

            // string downloadUrl = $"https://github.com/Cynteract/Connector/releases/download/v{packageVersion}/Connector.exe";
            // using (var webClient = new System.Net.WebClient())
            // {
            //     try
            //     {
            //         webClient.DownloadFile(downloadUrl, windowsPath);
            //     }
            //     catch (System.Exception e)
            //     {
            //         UnityEngine.Debug.LogError($"Failed to download Connector.exe from {downloadUrl}: {e.Message}");
            //     }
            // }
        }

        string androidPath = $"{downloadPath}/connector-release.aar";
        if (!File.Exists(androidPath))
        {
            UnityEngine.Debug.Log($"Downloading Connector.aar, version {packageVersion} for Android...");
            // Download via gh-cli. You need to run `gh auth login` first.
            string command = $"release download v{packageVersion} --repo Cynteract/Connector --pattern \"*.aar\" --dir {downloadPath}";
            string output = ExecuteCommand("gh", command);
            UnityEngine.Debug.Log(output);

            // string downloadUrl = $"https://github.com/Cynteract/Connector/releases/download/v{packageVersion}/connector-release.aar";
            // using (var webClient = new System.Net.WebClient())
            // {
            //     try
            //     {
            //         webClient.DownloadFile(downloadUrl, androidPath);
            //     }
            //     catch (System.Exception e)
            //     {
            //         UnityEngine.Debug.LogError($"Failed to download connector-release.aar from {downloadUrl}: {e.Message}");
            //     }
            // }
        }

    }
}
