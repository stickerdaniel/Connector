# Connector

Library that connects to the hardware via USB or BLE.

## Unity Frontend

Install https://cli.github.com/ and run `gh auth login`. Then open UPM and select "Add package from git URL...". Use the url `https://github.com/Cynteract/Connector.git?path=/frontend/Unity/Packages/com.cynteract.connector#main` . If you want to use a specific version, you specify it like this: `https://github.com/Cynteract/Connector.git?path=/frontend/Unity/Packages/com.cynteract.connector#v1.0.0` .

If you want to develop on this repository, clone it from `https://github.com/Cynteract/Connector.git`. To run the included sample, open the Unity project `frontend/Unity` and run it. It will log all input from the backend. You can also build the android apk to check it on your android phone. You can also import the Connector package into another Unity project via UPM -> "Add package from disc..." -> select `frontend\Unity\Packages\com.cynteract.connector\package.json`. This will create a link and you can develop it while working on another Unity project.

To debug the Unity scene on Android:
1. Open Windows Search and type "Environment variables". Select "Settings environmental variables" 
2. Select "path" and click on "Edit".
3. Now add the path to your Unity SDK folder as following: C:\Program Files\Unity\Hub\Editor\2022.3.16f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools
4. Enable Developer Mode on your Android device and enable "USB Debugging".
5. Open powershell and connect your phone via USB. Execute "adb devices" to confirm your device is connected.
6. Then run `adb tcpip 5555`. Check the IP Address in your phone's settings.
7. Run `adb connect 192.XXX.XXX.XXX`. Use your phone's IP Address.
8. Open Unity and switch to Android Platform in Build Settings. 
9. Select your phone in "Run Device" and then "Build and Run".
10. When finished Logcat will automatically open and display Debug Information. Start the App on the phone.

## Windows Backend

- Install vscode and the C# extension inside vscode.
- Install the .NET 8 sdk (~200MB) from here: https://dotnet.microsoft.com/en-us/download/dotnet/8.0 . If you have a newer sdk installed, it's sufficient to install the ".NET Runtime 8.\*" (~20MB) from that url.
- Enable script execution in powershell. Open windows start menu, type "powershell" and run powershell as administrator. Then run `Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser`
- Run the program with `dotnet run`.
- Debug the program with the "Play" button in vscode.
- Run the test with `dotnet run test`.
- Sync the binary to the Unity frontend with `.\tool.ps1 windows`.

To reset the glove, you have to physically disconnect and reconnect it.

You can use following lines as sample-input for the program:

```js
// trigger usb scan
{"type":"scan","connectionType":"usb"}

// send vibration data (all 50%)
{"type":"command","deviceId":"COM3","command":{"vibration":"MjIyMjIyMjIyMg==","vibrationPattern":"AAAAAAAAAAAAAA=="}}

// reset vibration
{"type":"command","deviceId":"COM3","command":{"vibration":"AAAAAAAAAAAAAA==","vibrationPattern":"AAAAAAAAAAAAAA=="}}

```

To show all compiler warnings, run:

```bat
dotnet build -t:"clean,build" Connector.csproj
```

The `windows.sln` is only kept in place because vscode won't highlight syntax errors otherwise.

## Android backend

- Install android studio from https://developer.android.com/studio (~1.1GB).
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

Sync the android-archive to the Unity frontend with `.\tool.ps1 android`.

## Releasing

Enable powershell script execution as mentioned in [Windows Backend](#windows-backend). Install https://cli.github.com/ . Update the "version" field in `frontend\Unity\Packages\com.cynteract.connector\package.json`. Then run

```powershell
# compile windows and android binaries
.\tool.ps1 all
# upload binaries to github
.\tool.ps1 release
```
