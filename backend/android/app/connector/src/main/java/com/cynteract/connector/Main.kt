package com.cynteract.connector

import android.content.Context

class Main(private val messageCallback: MessageListener) {
    val usb = Usb()
    val bluetooth: Ble = Ble()
    val deviceCache: DeviceCache =
        DeviceCache(usb, bluetooth)

    fun initialize(context: Context) {
        deviceCache.onMessageOut = { message -> message.write { messageCallback.onMessage(it) } }
        usb.init(context)
    }

    fun onCommand(command: String) {
        val message = Message.readLine(command)
        deviceCache.onMessageIn(message)
    }
}

interface MessageListener {
    fun onMessage(command: String)
}