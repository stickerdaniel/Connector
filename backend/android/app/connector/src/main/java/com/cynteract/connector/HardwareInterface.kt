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
    var onDeviceInformation: ((HardwareInterface, String, InformationV1In) -> Unit)?
    var onDeviceData: ((HardwareInterface, String, DataReceive) -> Unit)?
    var onDeviceDebug: ((HardwareInterface, String, String) -> Unit)?

    fun requestInformation(id: String)
    fun sendData(id: String, data: DataSend)
    fun close(context: Context)

    val connectionType: String
}
