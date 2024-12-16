
using System.IO;
using System.Diagnostics;
using System.Text;
using System;

using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public static class PackageSetup
{
    const string downloadPathWindows = "Assets/StreamingAssets";

    const string downloadPathAndroid = "Assets";

    static PackageSetup()
    {
        EditorApplication.delayCall += SyncReleaseVersions;
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

    private static void SyncReleaseVersions()
    {
        string packageVersion = GetPackageVersion();

        if (!AssetDatabase.IsValidFolder("Assets/StreamingAssets"))
        {
            UnityEngine.Debug.Log("Creating StreamingAssets folder...");
            AssetDatabase.CreateFolder("Assets", "StreamingAssets");
        }
        string windowsPath = $"{downloadPathWindows}/Connector_v{packageVersion}.exe";
        if (!File.Exists(windowsPath))
        {
            UnityEngine.Debug.Log($"Downloading Connector.exe, version {packageVersion} for Windows...");
            // Download via gh-cli. You need to run `gh auth login` first.
            string command = $"release download v{packageVersion} --repo Cynteract/Connector --pattern \"*.exe\" --output {windowsPath}";
            string output = ExecuteCommand("gh", command);
            UnityEngine.Debug.Log(output);
        }
        FilterOldFiles(
            folderPath:downloadPathWindows,
            currentFilepath: windowsPath,
            pattern:"Connector.*exe"
            );
        string androidPath = $"{downloadPathAndroid}/connector-release_v{packageVersion}.aar";
        if (!File.Exists(androidPath))
        {
            UnityEngine.Debug.Log($"Downloading Connector.aar, version {packageVersion} for Android...");
            // Download via gh-cli. You need to run `gh auth login` first.
            string command = $"release download v{packageVersion} --repo Cynteract/Connector --pattern \"*.aar\"  --output {androidPath}";
            string output = ExecuteCommand("gh", command);
            UnityEngine.Debug.Log(output);

        }
        FilterOldFiles(
            folderPath: downloadPathAndroid,
            currentFilepath: androidPath,
            pattern: "connector-release.*aar"
        );
    }

    private static void FilterOldFiles(string folderPath,string currentFilepath, string pattern)
    {
        var files = Directory.GetFiles(folderPath);

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            if (Regex.IsMatch(fileName, pattern))
            {
                if (fileName != Path.GetFileName(currentFilepath) && !fileName.Contains(".meta"))
                {
                    UnityEngine.Debug.Log($"Deleting other connector version: {fileName}");
                    File.Delete(file);
                    if (File.Exists($"{file}.meta"))
                    {
                        File.Delete($"{file}.meta");
                    }
                }
            }

        }
    }
}
