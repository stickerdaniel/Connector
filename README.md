# Connector

Library that connects to the hardware via USB or BLE.

## Windows Backend

- Install vscode and the C# extension inside vscode.
- Install the .net 7 sdk from here: https://dotnet.microsoft.com/en-us/download/dotnet .
- Run the program with `dotnet run`.
- Debug the program with the "Play" button in vscode.
- Run the test with `dotnet run test`.

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

To sync the program to the unity frontend, run:

```powershell
cd .\backend\windows
Copy-Item -Path ".\Main\PlatformInterface.cs" -Destination "..\..\frontend\unity_editor\UnityDeviceFrontend\Assets\PlatformInterface.cs" -Force
dotnet publish -c Release -r win10-x64 --no-self-contained -o ../../frontend/unity_editor/UnityDeviceFrontend/Assets/Connector_bin Connector.csproj
```

To show all compiler warnings, run:

```bat
dotnet build -t:"clean,build" Connector.csproj
```

The `windows.sln` is only kept in place because vscode won't highlight syntax errors otherwise.

## Windows Frontend

This is a Unity scene with a small test-script. You need to export the windows backend binaries first, as explained in "Windows Backend". The binaries are not committed into git.

## Android backend

- Download and install android studio
- Open project ./backend/android/connector/ in android studio
- In Android studio go to "Settings">"Languages & Frameworks">"Android SDK">"SDK Tools"> and install "Android SDK Platform-Tools"
- Connect your smart phone to android studio via wireless debugging
    - In Android studio click on "Device manager" then click on "Pair device using wifi"
    - Now in Android smart phone, Make sure to enable developer options
    - In "Settings">"Developer Options" turn on "Wireless debugging"
    - You can choose any option to pair with project, It is convinent to use "Pair device with QR code" 
    - Press on option "Pair device with  QR code", Now scan QR code which is visible in Android studio
- After connecting press "Run app" button in Android studio to run app on your mobile phone
- Open "Logcat", set filter to "package:com.cynteract.connector" to see all data logs and usb connection information
- Connect Data glove to smart phone via USB


To export project to unity create android archive for connector sub module

```powershell
cd .\backend\android
./gradlew :app:connector:assembleRelease   
Copy-Item -Path .\app\connector\build\outputs\aar\connector-release.aar -Destination .\..\..\frontend\unity_editor\UnityDeviceFrontend\Assets\Plugins -Force
```

## Exporting Unity package

In Unity Editor select "Assets">"Export package" Export all files in project.

## Importing Unity package

In Unity Editor select "Assets">"Import package">"Custom package" import all files into project.