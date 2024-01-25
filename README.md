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

```bat
copy /Y .\Main\PlatformInterface.cs ..\..\frontend\unity_editor\UnityDeviceFrontend\Assets\PlatformInterface.cs
dotnet publish -c Release -r win10-x64 --no-self-contained -o ../../frontend/unity_editor/UnityDeviceFrontend/Assets/Connector_bin Connector.csproj
```

To show all compiler warnings, run:

```bat
dotnet build -t:"clean,build" Connector.csproj
```

The `windows.sln` is only kept in place because vscode won't highlight syntax errors otherwise.

## Windows Frontend

This is a Unity scene with a small test-script. You need to export the windows backend binaries first, as explained in "Windows Backend". The binaries are not committed into git.