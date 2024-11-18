package com.cynteract.connector


class DeviceCache(usb: HardwareInterface, bluetooth: HardwareInterface) {


    var onMessageOut: ((Message) -> Unit)? = null

    data class Device(
        val deviceId: String,
        val connectionType: String,
        var isConnected: Boolean = false,
        var version: String? = null,
        var information: InformationV1Out? = null
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

    private fun HWOnDeviceInformation(
        sender: HardwareInterface,
        deviceId: String,
        informationIn: InformationV1In
    ) {
        synchronized(messageLock) {
            val device: Device = devices[deviceId] ?: return
            val informationOut: InformationV1Out = transformInformationV1InToOut(informationIn)
            if (device.information == null) {
                device.information = informationOut
                device.version = informationOut.version
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
                device.information = informationOut
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
                if (message.connectionType == ConnectionType.Usb) {
                    //hwInterfaces.usb.startScan()
                }
                // TODO: Handle Bluetooth
                // hwInterfaces.bluetooth.startScan()
            }
            is Message.InformationRequest->{
                val  deviceId=message.deviceId
                synchronized(messageLock){
                    val device=devices[deviceId]
                    if(device!=null){
                        hwInterfaces.usb.requestInformation(deviceId)
                    }
                }
            }
            is Message.Command -> {
                val deviceId = message.deviceId
                synchronized(messageLock) {
                    val device = devices[deviceId]
                    if (device != null) {
                        if (device.isConnected && device.connectionType == ConnectionType.Usb) {
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
