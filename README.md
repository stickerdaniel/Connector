# Connector

Library that connects to the hardware via USB or BLE.

## Prerequisites

Run following command in the terminal:

```powershell
winget install Microsoft.VisualStudioCode Unity.UnityHub Git.Git GitHub.cli Microsoft.DotNet.SDK.8 Google.AndroidStudio
```

Run `gh auth login` in a terminal and follow the login instructions.

Open Unity Hub, install the Editor version 2022.3.16f1.

Open windows settings, search for "Use developer features", expand "PowerShell" and change the toggle to "On".

Open vscode, go to extensions, select the filter "Recommended", click on "Install Workspace Recommended Extensions".

## Unity Frontend

Open UPM and select "Add package from git URL...". Use the url `https://github.com/Cynteract/Connector.git?path=/frontend/Unity/Packages/com.cynteract.connector#main` . If you want to use a specific version, you specify it like this: `https://github.com/Cynteract/Connector.git?path=/frontend/Unity/Packages/com.cynteract.connector#v1.0.0` .

If you want to develop on this repository, clone it from `https://github.com/Cynteract/Connector.git`. To run the included sample, open the Unity project `frontend/Unity` and run it. It will log all input from the backend. You can also build the android apk to check it on your android phone. You can also import the Connector package into another Unity project via UPM -> "Add package from disc..." -> select `frontend\Unity\Packages\com.cynteract.connector\package.json`. This will create a link and you can develop it while working on another Unity project.

To debug the Unity scene on Android:

1. Open Windows Search and type "Environment variables". Select "Settings environmental variables"
2. Select "path" and click on "Edit".
3. Now add the path to your Unity SDK folder as following: C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools
4. Enable Developer Mode on your Android device.
5. Search for "WLAN Debugging" on your Android phone and enable it.
6. Remember the ip and port in the "WLAN Debugging" settings.
7. Open powershell on your computer and run `adb connect 192.XXX.XXX.XXX:XXX`. Use your phone's ip address and port number.
8. Open Unity and switch to Android Platform in Build Settings.
9. Select your phone in "Run Device" and then "Build and Run".
10. When finished Logcat will automatically open and display Debug Information. Start the App on the phone.

As alternative to enabling "WLAN Debugging" via android settings, you can enable "USB debugging" instead, connect your device via USB cable and run `adb tcpip 5555` and `adb connect 192.XXX.XXX.XXX`.

## Windows Backend

Run the backend with the task `Windows Watch Run`. The running instance will hot update on every code change.

Debug the backend with the launch configuration `Connector`.

Run the tests from the test explorer. To debug a test, hover the test and click "Debug Test".

Sync the binary to the Unity frontend with the task `Windows Publish`.

To reset the glove, you have to physically disconnect and reconnect it.

You can use following lines as sample-input for the program:

```js
// trigger usb scan
{"type":"scan","connectionType":"usb"}

// send vibration data (all 50%)
{"type":"command","deviceId":"COM9","command":{"vibration":[50,50,50,50,50,50,50,50,50,50],"vibrationPattern":[0,0,0,0,0,0,0,0,0,0]}}

// reset vibration
{"type":"command","deviceId":"COM9","command":{"vibration":[0,0,0,0,0,0,0,0,0,0],"vibrationPattern":[0,0,0,0,0,0,0,0,0,0]}}

```

For error highlighting, run `.NET: Open Solution` and choose `backend\backend\Connector.sln`.

## Android backend

- In Android studio go to "File">"Settings">"Languages & Frameworks">"Android SDK"
- If you already installed the android sdk with Unity, set the path of the sdk to Unity's android sdk.
  - Install android sdk 34.
  - If you get: "The SDK directory is not writable (C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK)", you need to grant write permissions.
  - Rightlick on the SDK folder in windows explorer, go to "Properties">"Security">"Modify", select the currently logged in user and grant full access.
- If you don't have Unity's android sdk installed, proceed to >"SDK Tools"> and install "Android SDK Platform-Tools".
- Open project `./backend/android/connector` in android studio
- Connect your smart phone to android studio via wireless debugging
  - In Android studio click on "Device manager" then click on "Pair device using wifi"
  - Now in Android smart phone, Make sure to enable developer options
  - In "Settings">"Developer Options" turn on "Wireless debugging"
  - You can choose any option to pair with project, It is convinent to use "Pair device with QR code"
  - Press on option "Pair device with QR code", Now scan QR code which is visible in Android studio
- After connecting press "Run app" button in Android studio to run app on your mobile phone
- Open "Logcat", set filter to "package:com.cynteract.connector" to see all data logs and usb connection information
- Connect Data glove to smart phone via USB

Sync the android-archive to the Unity frontend with the task `Android Build`.

## Releasing

Update the "version" field in `frontend\Unity\Packages\com.cynteract.connector\package.json` and create a commit. Then run the tasks `Build All` and `Release`.
