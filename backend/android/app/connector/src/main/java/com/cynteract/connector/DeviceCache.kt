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
            devices.computeIfAbsent(deviceId) {
                Device(deviceId, sender.connectionType)
            }.apply {
                if (!isConnected) {
                    isConnected = true
                    if (information == null) {
                        sender.requestInformation(deviceId)
                    } else {
                        onMessageOut?.invoke(
                            Message.Connect(
                                deviceId = deviceId,
                                connectionType = connectionType,
                                isConnected = isConnected,
                                version = version!!,
                                information = information!!
                            )
                        )
                    }
                }
            }
        }
    }

    private fun HWOnDeviceDisconnected(sender: HardwareInterface, deviceId: String) {
        synchronized(messageLock) {
            devices[deviceId]?.apply {
                if (isConnected) {
                    isConnected = false
                    onMessageOut?.invoke(Message.Disconnect(deviceId = deviceId))
                }
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
            devices[deviceId]?.apply {
                if (this.information == null) {
                    this.information = information
                    val version = information.version
                    onMessageOut?.invoke(
                        Message.Connect(
                            deviceId = deviceId,
                            connectionType = connectionType,
                            isConnected = isConnected,
                            version = version,
                            information = this.information!!
                        )
                    )
                } else {
                    this.information = information
                }
            }
        }
    }

    private fun HWOnDeviceData(sender: HardwareInterface, deviceId: String, data: DataReceive) {
        // only propagate data after receiving information
        synchronized(messageLock) {
            devices[deviceId]?.apply {
                if (version != null) {
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
                    devices[deviceId]?.apply {
                        if (isConnected && connectionType == ConnectionType.Usb.name) {
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
