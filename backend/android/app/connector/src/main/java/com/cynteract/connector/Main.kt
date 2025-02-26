package com.cynteract.connector

import android.content.Context
import com.cynteract.connector.usb.UsbReceiver


class Main(private val messageCallback: MessageListener) {
    val usb = UsbReceiver()
    val bluetooth: Ble = Ble()
    val deviceCache: DeviceCache =
        DeviceCache(usb, bluetooth)
    private val syncLock = Any()

    fun initialize(context: Context) {
        deviceCache.onMessage = { deviceId, message ->
            messageCallback.onMessage(
                Protocol.serialize(
                    deviceId,
                    message
                )
            )
        }
        usb.init(context)
    }

    fun close(context: Context) {
        usb.close(context)
    }

    fun sendMessage(messageLine: String) {
        val (deviceId, message) = Protocol.deserialize(messageLine)
        synchronized(syncLock) {
            deviceCache.sendMessage(deviceId, message)
        }
    }
}

interface MessageListener {
    fun onMessage(message: String)
}