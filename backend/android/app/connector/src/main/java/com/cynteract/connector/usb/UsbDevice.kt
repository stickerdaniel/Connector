package com.cynteract.connector.usb

import com.cynteract.connector.HardwareProtocol
import com.cynteract.connector.Log
import com.hoho.android.usbserial.driver.UsbSerialPort
import java.io.IOException
import java.nio.BufferUnderflowException
import java.nio.ByteBuffer
import java.util.concurrent.ConcurrentLinkedQueue

class UsbDevice(
    val deviceName: String,
    val serial: UsbSerialPort,
    val openConnection: () -> Unit
) {

    private val readThread: Thread = Thread { readRoutine() }
    private val writeThread: Thread = Thread { writeRoutine() }
    private val startThread: Thread = Thread { startRoutine() }
    private val writeLock = Any()
    private val messageSendQueue: ConcurrentLinkedQueue<Any> = ConcurrentLinkedQueue()

    private class PackageReadBuffer {
        var transmissionStartTime: Long = 0
        var data = ByteArray(1024)
        var offset = 0
    }

    private val readBuffer = PackageReadBuffer()

    companion object {
        val PACKAGE_DELIM = "CYNTERACT\n".toByteArray()
    }

    private val hardwareProtocol = HardwareProtocol()

    // throttle sending data
    private var writeTimestamp = 0L

    var running = false
    var onRequestConnectionCheck: ((String) -> Unit)? = null
    var onDeviceConnected: ((String) -> Unit)? = null
    var onDeviceError: ((String, String) -> Unit)? = null
    var onDeviceMessage: ((String, Any) -> Unit)? = null

    fun bufferSize(): Int {
        return readBuffer.data.size
    }

    fun packageTimeout(): Long {
        return 2000
    }

    fun start() {
        Log.d(deviceName, "Starting device")
        running = true
        startThread.start()
    }

    private fun startRoutine() {
        while (!tryStart()) {
            try {
                Thread.sleep(500)
            } catch (e: InterruptedException) {
                onDeviceError?.invoke(deviceName, "Start routine interrupted: ${e.message}")
                return
            }
        }
    }

    private fun tryStart(): Boolean {
        try {
            openConnection()
            serial.setParameters(
                230400,
                8,
                UsbSerialPort.STOPBITS_1,
                UsbSerialPort.PARITY_NONE
            )
            onDeviceConnected?.invoke(deviceName)
            readThread.start()
            writeThread.start()
            return true
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error opening device: ${e.message}")
            return false
        }
    }

    fun close() {
        try {
            serial.close()
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error closing device: ${e.message}")
        }
        startThread.interrupt()
        readThread.interrupt()
        writeThread.interrupt()
        startThread.join()
        readThread.join()
        writeThread.join()

        running = false
    }

    private fun receiveMessage() {
        val buffer = ByteArray(512)
        val numBytesRead = serial.read(buffer, 1000)
        for (i in 0 until numBytesRead) {
            var x = buffer[i]

            // case 1: last package timed out
            if (readBuffer.transmissionStartTime > 0 && System.currentTimeMillis() > readBuffer.transmissionStartTime + packageTimeout()) {
                onDeviceError?.invoke(deviceName, "Package timeout.")
                // assume the start of a new package
                readBuffer.transmissionStartTime = 0
            }
            // case 2: transmission of new package
            if (readBuffer.transmissionStartTime == 0L) {
                readBuffer.offset = 0
                readBuffer.transmissionStartTime = System.currentTimeMillis()
            }
            // case 3: readBuffer overflow, this package will be corrupted
            if (readBuffer.offset == readBuffer.data.size) {
                onDeviceError?.invoke(deviceName, "ReadBuffer overflow.")
                // continue reading the package until the end, even if it is corrupted
                readBuffer.offset = 0
            }
            // case 4: continuation of already started package
            readBuffer.data[readBuffer.offset++] = x
            // case 5: end of package reached
            if (readBuffer.offset >= PACKAGE_DELIM.size &&
                readBuffer.data.copyOfRange(
                    readBuffer.offset - PACKAGE_DELIM.size,
                    readBuffer.offset
                ).contentEquals(PACKAGE_DELIM)
            ) {
                readBuffer.transmissionStartTime = 0
                val byteBuffer =
                    ByteBuffer.wrap(readBuffer.data, 0, readBuffer.offset - PACKAGE_DELIM.size)
                try {
                    val message = hardwareProtocol.deserialize(byteBuffer)
                    onDeviceMessage?.invoke(deviceName, message)
                } catch (e: Exception) {
                    when (e) {
                        is IllegalArgumentException, is BufferUnderflowException -> {
                            onDeviceError?.invoke(
                                deviceName,
                                "Error deserializing device input: ${e.message}"
                            )
                            Thread.sleep(10)
                        }

                        else -> throw e
                    }
                }
            }

        }
    }


    fun sendMessage(message: Any) {
        messageSendQueue.add(message)
    }

    private fun sendMessageImmediately(message: Any) {
        // throttle data sending
        val now = System.currentTimeMillis()
        if (now - writeTimestamp < 10) {
            onDeviceError?.invoke(deviceName, "Dropped command due to throttling")
            return
        }
        writeTimestamp = now
        val buffer = ByteBuffer.allocate(512)
        hardwareProtocol.serialize(buffer, message)
        try {
            synchronized(writeLock) {
                serial.write(buffer.array(), 500)
                serial.write(PACKAGE_DELIM, 500)
            }
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error writing data: ${e.message}")
        }
    }


    private fun readRoutine() {
        while (!Thread.currentThread().isInterrupted) {
            try {
                try {
                    receiveMessage()
                } catch (e: IOException) {
                    onDeviceError?.invoke(deviceName, "Error reading device: ${e.message}")
                    // check if the device is still connected
                    onRequestConnectionCheck?.invoke(deviceName)
                    Thread.sleep(500)
                }

                Thread.sleep(5)
            } catch (e: InterruptedException) {
                onDeviceError?.invoke(deviceName, "Read routine interrupted: ${e.message}")
                return
            }
        }
    }

    private fun writeRoutine() {
        while (!Thread.currentThread().isInterrupted) {
            try {
                //Only request information once
                var requestedInformation = false
                while (!messageSendQueue.isEmpty()) {
                    val data = messageSendQueue.remove()
                    sendMessageImmediately(data)
                    Thread.sleep(50)
                }
                Thread.sleep(5)
            } catch (e: InterruptedException) {
                onDeviceError?.invoke(deviceName, "Write routine interrupted: ${e.message}")
                return
            }
        }
    }
}