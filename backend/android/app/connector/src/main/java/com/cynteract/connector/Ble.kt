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
    override var onDeviceMessage: ((HardwareInterface, String, Any) -> Unit)?
        get() = TODO("Not yet implemented")
        set(value) {}

    override fun sendMessage(id: String, data: Any) {
        TODO("Not yet implemented")
    }

    override fun close(context: Context) {
        TODO("Not yet implemented")
    }

    override val connectionType: String
        get() = TODO("Not yet implemented")
}