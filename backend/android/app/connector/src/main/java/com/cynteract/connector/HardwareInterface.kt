package com.cynteract.connector

import android.content.Context


object ConnectionType {
    const val Usb = "usb"
    const val Bluetooth = "bluetooth"
}

interface HardwareInterface {
    var onDeviceConnected: ((HardwareInterface, String) -> Unit)?
    var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)?
    var onDeviceError: ((HardwareInterface, String, String) -> Unit)?
    var onDeviceMessage: ((HardwareInterface, deviceId: String, message: Any) -> Unit)?
    fun sendMessage(deviceId: String, message: Any)
    fun close(context: Context)
    val connectionType: String
}
