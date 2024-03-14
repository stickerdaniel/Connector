package com.cynteract.connector

enum class ConnectionType {
    Usb,
    Bluetooth
}

class DeviceCache(usb: HardwareInterface, bluetooth: HardwareInterface) {


    var onMessageOut: ((Message) -> Unit)? = null

    data class Device(
        val deviceId: String,
        val connectionType: String,
        var isConnected: Boolean = false,
        var version: String? = null,
        var information: Information? = null
    )

    private val devices = mutableMapOf<String, Device>()

    class HWInterfaces(val usb: HardwareInterface, val bluetooth: HardwareInterface)

    private val hwInterfaces = HWInterfaces(usb, bluetooth)

    init {
        for (hwi in listOf(hwInterfaces.usb, hwInterfaces.bluetooth)) {
            hwi.onDeviceConnected = { sender, deviceId -> HWOnDeviceConnected(sender, deviceId) }
            hwi.onDeviceDisconnected =
                { sender, deviceId -> HWOnDeviceDisconnected(sender, deviceId) }
            hwi.onDeviceError =
                { sender, deviceId, message -> HWOnDeviceError(sender, deviceId, message) }
            hwi.onDeviceInformation = { sender, deviceId, information ->
                HWOnDeviceInformation(
                    sender,
                    deviceId,
                    information
                )
            }
            hwi.onDeviceData = { sender, deviceId, data -> HWOnDeviceData(sender, deviceId, data) }
            hwi.onDeviceDebug =
                { sender, deviceId, message -> HWOnDeviceDebug(sender, deviceId, message) }
        }
    }

    private val messageLock = Any()

    private fun HWOnDeviceConnected(sender: HardwareInterface, deviceId: String) {
        synchronized(messageLock) {
            val device = devices.computeIfAbsent(deviceId) {
                Device(deviceId, sender.connectionType)
            }
            if (!device.isConnected) {
                device.isConnected = true
                if (device.information == null) {
                    sender.requestInformation(deviceId)
                } else {
                    onMessageOut?.invoke(
                        Message.Connect(
                            deviceId = deviceId,
                            connectionType = device.connectionType,
                            isConnected = device.isConnected,
                            version = device.version!!,
                            information = device.information!!
                        )
                    )
                }
            }
        }
    }

    private fun HWOnDeviceDisconnected(sender: HardwareInterface, deviceId: String) {
        synchronized(messageLock) {
            val device: Device = devices[deviceId] ?: return
            if (device.isConnected) {
                device.isConnected = false
                onMessageOut?.invoke(Message.Disconnect(deviceId = deviceId))
            }
        }
    }

    private fun HWOnDeviceError(sender: HardwareInterface, deviceId: String, message: String) {
        onMessageOut?.invoke(Message.Error(deviceId = deviceId, message = message))
    }

    private fun HWOnDeviceInformation(
        sender: HardwareInterface,
        deviceId: String,
        information: Information
    ) {
        synchronized(messageLock) {
            val device: Device = devices[deviceId] ?: return
            if (device.information == null) {
                device.information = information
                // field "version" was missing in v2 firmware
                if (information.version == "") {
                    val version = "1"
                    device.information = Information(
                        Hand = information.Hand,
                        version = version,
                    )
                    device.version = version
                } else {
                    device.version = information.version
                }
                onMessageOut?.invoke(
                    Message.Connect(
                        deviceId = deviceId,
                        connectionType = device.connectionType,
                        isConnected = device.isConnected,
                        version = device.information!!.version,
                        information = device.information!!
                    )
                )
            } else {
                device.information = information
            }
        }
    }

    private fun HWOnDeviceData(sender: HardwareInterface, deviceId: String, data: DataReceive) {
        // only propagate data after receiving information
        synchronized(messageLock) {
            val device: Device = devices[deviceId] ?: return
            if (device.information == null) {
                // there was no information received yet, request it again
                sender.requestInformation(deviceId)
            } else
                if (device.version == "1") {
                    onMessageOut?.invoke(
                        Message.Data(
                            deviceId = deviceId,
                            data = Dataframe(
                                force = data.force,
                                imu = data.imu.map { quaternion ->
                                    Dataframe.IMUData(
                                        x = quaternion.x,
                                        y = quaternion.y,
                                        z = quaternion.z,
                                        w = quaternion.w
                                    )
                                }.toTypedArray(),
                                imuStatus = data.imuStatus,
                                vibStatus = data.vibStatus
                            )
                        )
                    )
                }
        }
    }


    private fun HWOnDeviceDebug(sender: HardwareInterface, deviceId: String, message: String) {
        onMessageOut?.invoke(Message.Debug(deviceId = deviceId, message = message))
    }

    fun onMessageIn(message: Message) {
        when (message) {
            is Message.Scan -> {
                if (message.connectionType == ConnectionType.Usb.name) {
                    //hwInterfaces.usb.startScan()
                }
                // TODO: Handle Bluetooth
                // hwInterfaces.bluetooth.startScan()
            }

            is Message.Command -> {
                val deviceId = message.deviceId
                synchronized(messageLock) {
                    val device = devices[deviceId]
                    if (device != null) {
                        if (device.isConnected && device.connectionType == ConnectionType.Usb.name) {
                            hwInterfaces.usb.sendData(
                                deviceId, DataSend(
                                    vibration = message.command.vibration,
                                    vibrationPattern = message.command.vibrationPattern
                                )
                            )
                        }
                    }
                }
            }

            else -> throw Exception("Unknown message type: ${message.type}")
        }
    }

}
