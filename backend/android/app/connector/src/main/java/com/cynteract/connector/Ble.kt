package com.cynteract.connector

import android.content.Context

class Ble : HardwareInterface {
    override var onDeviceConnected: ((HardwareInterface, String) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}
    override var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}
    override var onDeviceError: ((HardwareInterface, String, String) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}
    override var onDeviceInformation: ((HardwareInterface, String, InformationV1In) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}
    override var onDeviceData: ((HardwareInterface, String, DataReceive) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}
    override var onDeviceDebug: ((HardwareInterface, String, String) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}

    override fun requestInformation(id: String) {
        TODO("Not yet implemented")
    }

    override fun sendData(id: String, data: DataSend) {
        TODO("Not yet implemented")
    }

    override fun close(context: Context) {
        TODO("Not yet implemented")
    }

    override val connectionType: String
        get() = TODO("Not yet implemented")
}