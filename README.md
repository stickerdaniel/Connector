# Connector

Library that connects to the hardware via USB or BLE.

## Unity Frontend

To run the provided sample scene or to import the package into another Unity scene, you have to create the windows and android backend binaries first. Those binaries are not committed into git to keep the history clean. 

Install the .net 7 sdk (~200MB) from here: https://dotnet.microsoft.com/en-us/download/dotnet/7.0 . If you have a newer sdk installed, you can also use that. In that case you only need to download and install the ".NET Runtime 7.*" (~20MB) from that url.

Install the android sdk. It is recommended to install android studio first and then install the android sdk via android studio. See [Android backend](#android-backend) on how to install the sdk via android studio.

Clone this repository (`git clone git@github.com:Cynteract/Connector.git`), open powershell and run following commands:

```powershell
# powershell

# compile windows backend
cd .\backend\windows
dotnet publish -c Release -r win10-x64 --no-self-contained -o ../../frontend/Unity/Assets/Plugins/Windows Connector.csproj

# compile android backend
cd ..\android
./gradlew :app:connector:assembleRelease   
Copy-Item -Path .\app\connector\build\outputs\aar\connector-release.aar -Destination .\..\..\frontend\Unity\Assets\Plugins\Android -Force
```

To run the included sample, open the Unity project `frontend/Unity` and run it. It will log all input from the backend. You can also build the android apk to check it on your android phone.

To import the package into another Unity project, open Unity's Package Manager, click the "+"-button in the status bar, select "Add package from disk", navigate to `frontend\Unity\Packages\com.cynteract.connector` and double-click the `package.json` file in the file browser. Then copy the folder `frontend/Unity/Assets/Plugins` into the root of the `Assets` folder of the other Unity project. Those can not be imported via Unity's package manager yet. 

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
Copy-Item -Path ".\Main\PlatformInterface.cs" -Destination "..\..\frontend\Unity\Packages\com.cynteract.connector\Runtime" -Force
dotnet publish -c Release -r win10-x64 --no-self-contained -o ../../frontend/Unity/Assets/Plugins/Windows Connector.csproj
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
    - Install android sdk 33. 
    - If you get: "The SDK directory is not writable (C:\Program Files\Unity\Hub\Editor\2022.3.11f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK)", you need to grant write permissions.
    - Rightlick on the SDK folder in windows explorer, go to "Properties">"Security">"Modify", select the currently logged in user and grant full access.
- If you don't have Unity's android sdk installed, proceed to >"SDK Tools"> and install "Android SDK Platform-Tools".
- Open project `./backend/android/connector` in android studio
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
Copy-Item -Path .\app\connector\build\outputs\aar\connector-release.aar -Destination .\..\..\frontend\Unity\Assets\Plugins\Android -Force
```
