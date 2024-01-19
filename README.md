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
{"type":"scan","body":"usb"}

// send vibration data (all 50%)
{"type":"data","body":{"id":"COM3","data":{"vibration":"MjIyMjIyMjIyMg==","vibrationPattern":"AAAAAAAAAAAAAA==","requestInformation":false}}}

// reset vibration
{"type":"data","body":{"id":"COM3","data":{"vibration":"AAAAAAAAAAAAAA==","vibrationPattern":"AAAAAAAAAAAAAA==","requestInformation":false}}}

```