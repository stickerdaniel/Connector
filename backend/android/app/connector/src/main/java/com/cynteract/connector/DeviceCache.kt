package com.cynteract.connector


class DeviceCache(usb: HardwareInterface, bluetooth: HardwareInterface) {


    var onMessageOut: ((Message) -> Unit)? = null

    data class Device(
        val deviceId: String,
        val connectionType: String
    )

    private val devices = mutableMapOf<String, Device>()

    class HWInterfaces(val usb: HardwareInterface, val bluetooth: HardwareInterface)

    private val hwInterfaces = HWInterfaces(usb, bluetooth)

    init {
        for (hwi in listOf(hwInterfaces.usb, hwInterfaces.bluetooth)) {
            hwi.onDeviceConnected =
                { sender, deviceId -> hardwareOnDeviceConnected(sender, deviceId) }
            hwi.onDeviceDisconnected =
                { sender, deviceId -> hardwareOnDeviceDisconnected(sender, deviceId) }
            hwi.onDeviceError =
                { sender, deviceId, message -> hardwareOnDeviceError(sender, deviceId, message) }
            hwi.onDeviceInformation = { sender, deviceId, information ->
                hardwareOnDeviceInformation(
                    sender,
                    deviceId,
                    information
                )
            }
            hwi.onDeviceData = { sender, deviceId, data -> hardwareOnDeviceData(sender, deviceId, data) }
            hwi.onDeviceDebug =
                { sender, deviceId, message -> hardwareOnDeviceDebug(sender, deviceId, message) }
        }
    }

    private val messageLock = Any()
    private fun transformIndexDictToList(dict: Map<String, String>): List<String> {
        val maxKey = dict.keys.map { it.toInt() }.maxOrNull() ?: 0
        val list = MutableList(maxKey + 1) { "" }
        for (item in dict) {
            list[item.key.toInt()] = item.value
        }
        return list
    }

    private fun transformInformationV1InToOut(information: InformationV1In): InformationV1Out {
        return InformationV1Out(
            version = "1",
            hand = information.Hand,
            vibration = transformIndexDictToList(information.Vibration),
            imu = transformIndexDictToList(information.IMU)
        )
    }

    private fun hardwareOnDeviceConnected(sender: HardwareInterface, deviceId: String) {
        val device = devices.computeIfAbsent(deviceId) {
            Device(deviceId, sender.connectionType)
        }
        sender.requestInformation(deviceId)
        onMessageOut?.invoke(
            Message.Connect(
                deviceId = deviceId,
                connectionType = device.connectionType
            )
        )
    }

    private fun hardwareOnDeviceDisconnected(sender: HardwareInterface, deviceId: String) {
        devices.remove(deviceId)
        onMessageOut?.invoke(Message.Disconnect(deviceId = deviceId))
    }

    private fun hardwareOnDeviceError(
        sender: HardwareInterface,
        deviceId: String,
        message: String
    ) {
        onMessageOut?.invoke(Message.Error(deviceId = deviceId, message = message))
    }


    private fun hardwareOnDeviceInformation(
        sender: HardwareInterface,
        deviceId: String,
        informationIn: InformationV1In
    ) {
        val informationOut: InformationV1Out = transformInformationV1InToOut(informationIn)
        onMessageOut?.invoke(
            Message.InformationMessage(deviceId, informationOut)
        )
    }

    private fun hardwareOnDeviceData(sender: HardwareInterface, deviceId: String, data: DataReceive) {
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


    private fun hardwareOnDeviceDebug(sender: HardwareInterface, deviceId: String, message: String) {
        onMessageOut?.invoke(Message.Debug(deviceId = deviceId, message = message))
    }

    fun onMessageIn(message: Message) {
        when (message) {
            is Message.Scan -> {
                if (message.connectionType == ConnectionType.Usb) {
                    //hwInterfaces.usb.startScan()
                }
                // TODO: Handle Bluetooth
                // hwInterfaces.bluetooth.startScan()
            }

            is Message.InformationRequest -> {
                val deviceId = message.deviceId
                synchronized(messageLock) {
                    val device = devices[deviceId]
                    if (device != null) {
                        hwInterfaces.usb.requestInformation(deviceId)
                    }
                }
            }

            is Message.Command -> {
                val deviceId = message.deviceId
                synchronized(messageLock) {
                    val device = devices[deviceId]
                    if (device != null) {
                        if (device.connectionType == ConnectionType.Usb) {
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
