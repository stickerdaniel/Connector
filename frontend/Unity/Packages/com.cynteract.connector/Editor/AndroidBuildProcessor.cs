// https://forum.unity.com/threads/gradle-build-error-gradle-version-2-10-is-required-current-version-is-4-0-1.499520/#post-4734422
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;


using UnityEditor.Android;
using System.IO;

namespace Connector
{
    class GradlePostProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder { get { return 0; } }
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Adds the following line to gradle.properties:
            // android.useAndroidX=true
            EnableAndroidX(path);
            // Adds the following repository to settings.gradle:
            // maven{url = "https://jitpack.io"}
            AddJitpackRepository(path);
            // Adds connector dependencies to build.gradle, taken from backend\android\app\connector\build.gradle.kts
            AddDependencies(path);
        }

        private void EnableAndroidX(string path)
        {
            string gradlePropertiesPath = path + "/../gradle.properties";
            string[] lines = File.ReadAllLines(gradlePropertiesPath);
            bool hasAndroidXProperty = lines.Any(text => text.Contains("android.useAndroidX"));
            if (!hasAndroidXProperty)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string each in lines)
                {
                    builder.AppendLine(each);
                }
                builder.AppendLine("# AndroidX required for Connector");
                builder.AppendLine("android.useAndroidX=true");
                File.WriteAllText(gradlePropertiesPath, builder.ToString());
            }
        }

        private void AddJitpackRepository(string path)
        {
            string settingsGradlePath = path + "/../settings.gradle";
            string[] lines = File.ReadAllLines(settingsGradlePath);
            bool hasJitpackRepository = lines.Any(text => text.Contains("https://jitpack.io"));
            if (!hasJitpackRepository)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string each in lines)
                {
                    // inject jitpack repository before flatDir
                    if (each.Contains("flatDir"))
                    {
                        builder.AppendLine("        // Jitpack repository required for Connector");
                        builder.AppendLine("        maven{url = 'https://jitpack.io'}");
                    }
                    builder.AppendLine(each);
                }
                File.WriteAllText(settingsGradlePath, builder.ToString());
            }
        }

        private void AddDependencies(string path)
        {
            string buildGradlePath = path + "/build.gradle";
            string[] lines = File.ReadAllLines(buildGradlePath);
            string dependencies = @"
    // Connector dependencies
    implementation(platform(""org.jetbrains.kotlin:kotlin-bom:1.8.0""))
    implementation(""androidx.core:core-ktx:1.10.1"")
    implementation(""androidx.appcompat:appcompat:1.0.2"")
    implementation(""com.github.mik3y:usb-serial-for-android:3.7.0"")
    implementation(""org.jetbrains.kotlinx:kotlinx-serialization-json:1.6.0"")
    implementation(""org.jetbrains.kotlin:kotlin-reflect:1.9.22"")";
            bool hasConnectorDependency = lines.Any(text => text.Contains("com.github.mik3y:usb-serial-for-android"));
            if (!hasConnectorDependency)
            {
                StringBuilder builder = new StringBuilder();
                foreach (string each in lines)
                {
                    builder.AppendLine(each);
                    // inject connector dependencies after connector-release dependency
                    if (each.Contains("connector-release"))
                    {
                        builder.AppendLine(dependencies);
                    }
                }
                File.WriteAllText(buildGradlePath, builder.ToString());
            }
        }
    }
}
