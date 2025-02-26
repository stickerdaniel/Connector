package com.cynteract.connector

import com.cynteract.connector.messages.Disconnect
import com.cynteract.connector.messages.Error
import com.cynteract.connector.messages.Scan


class DeviceCache(usb: HardwareInterface, bluetooth: HardwareInterface) {


    var onMessage: ((String, Any) -> Unit)? = null

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
            hwi.onDeviceMessage =
                { sender, deviceId, message ->
                    hardwareOnDeviceMessage(
                        sender,
                        deviceId,
                        message
                    )
                }
        }
    }

    private val messageLock = Any()

    private fun hardwareOnDeviceConnected(sender: HardwareInterface, deviceId: String) {
        val device = devices.computeIfAbsent(deviceId) {
            Device(deviceId, sender.connectionType)
        }
        onMessage?.invoke(
            deviceId,
            com.cynteract.connector.messages.Connect(
                connectionType = device.connectionType
            )
        )
    }

    private fun hardwareOnDeviceDisconnected(sender: HardwareInterface, deviceId: String) {
        devices.remove(deviceId)
        onMessage?.invoke(deviceId, Disconnect())
    }

    private fun hardwareOnDeviceError(
        sender: HardwareInterface,
        deviceId: String,
        message: String
    ) {
        onMessage?.invoke(
            deviceId,
            Error(
                message = message
            )
        )
    }

    private fun hardwareOnDeviceMessage(
        sender: HardwareInterface,
        deviceId: String,
        deviceMessage: Any
    ) {
        onMessage?.invoke(
            deviceId,
            deviceMessage
        )
    }

    fun sendMessage(deviceId: String?, message: Any) {
        when (message) {
            is Scan -> {
                if (message.connectionType == ConnectionType.Usb) {
                    //hwInterfaces.usb.startScan()
                }
                // TODO: Handle Bluetooth
                // hwInterfaces.bluetooth.startScan()
            }

            else
                -> {
                val device = devices[deviceId]
                if (device != null) {
                    when (device.connectionType) {
                        ConnectionType.Usb -> {
                            hwInterfaces.usb.sendMessage(deviceId!!, message)
                        }

                        ConnectionType.Bluetooth -> {
                            hwInterfaces.bluetooth.sendMessage(deviceId!!, message)
                        }
                    }
                }
            }
        }
    }

}
