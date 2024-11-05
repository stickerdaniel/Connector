package com.cynteract.connector

import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbManager
import android.util.Log
import com.hoho.android.usbserial.driver.UsbSerialDriver
import com.hoho.android.usbserial.driver.UsbSerialPort
import java.io.IOException
import java.util.concurrent.ConcurrentLinkedQueue

class SerialDevice(
    val deviceName:String,
    val driver: UsbSerialDriver,
    val serial: UsbSerialPort,
    val usbManager: UsbManager
) {

    private val readThread: Thread = Thread { readRoutine() }
    private val writeThread: Thread = Thread { writeRoutine() }
    private val packageReadBuffer = PackageReadBuffer()
    private val packageSendBuffer = ByteArray(Protocol.DATA_SEND_SIZE)
    private val writeLock = Any()

    private val dataSendQueue: ConcurrentLinkedQueue<DataSend> = ConcurrentLinkedQueue()

    var onDeviceConnected: ((String) -> Unit)? = null
    var onDeviceError: ((String, String) -> Unit)? = null
    var onDeviceInformation: ((String, InformationV1In) -> Unit)? = null
    var onDeviceData: ((String, DataReceive) -> Unit)? = null
    var onDeviceDebug: ((String, String) -> Unit)? = null
    var onRequestConnectionCheck: ((String) -> Unit)? = null;

    // throttle sending data
    private var writeTimestamp = 0L

    public var running=false
    public fun start() {
        Log.d(deviceName, "Starting device")
        running=true
        try {
            //if (!device.serial.isOpen) {
            val connection: UsbDeviceConnection = usbManager.openDevice(driver.device)
                ?: throw IOException("Cannot open device")
            serial.open(connection)
            serial.setParameters(
                230400,
                8,
                UsbSerialPort.STOPBITS_1,
                UsbSerialPort.PARITY_NONE
            )
            //}
            onDeviceConnected?.invoke(deviceName)
            readThread.start()
            writeThread.start()
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error opening device: ${e.message}")
        }
    }

    public fun close() {
        try {
            serial.close()
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error closing device: ${e.message}")
        }
        readThread.interrupt()
        writeThread.interrupt()

        running=false
    }

    private fun readPackage(x: Byte, buffer: PackageReadBuffer) {

        // case 1: last package timed out
        if (buffer.transmissionStartTime > 0 && System.currentTimeMillis() > buffer.transmissionStartTime + 2000) {
            System.err.println("package timed out")
            // assume the start of a new package
            buffer.transmissionStartTime = 0
        }
        // case 2: transmission of new package
        if (buffer.transmissionStartTime == 0L) {
            buffer.offset = 0
            buffer.transmissionStartTime = System.currentTimeMillis()
        }
        // case 3: buffer overflow, this package will be corrupted
        if (buffer.offset == buffer.data.size) {
            System.err.println("buffer overflow")
            // continue reading the package until the end, even if it is corrupted
            buffer.offset = 0
        }
        // case 4: continuation of already started package
        buffer.data[buffer.offset++] = x
        // case 5: end of package reached
        if (buffer.offset >= Protocol.PACKAGE_DELIM.size &&
            Protocol.memoryCompare(
                Protocol.PACKAGE_DELIM,
                buffer.data,
                buffer.offset - Protocol.PACKAGE_DELIM.size
            )
        ) {
            //Console.Error.WriteLine(b.offset);
            buffer.transmissionStartTime = 0
            if (Protocol.memoryCompare("DATA".toByteArray(), buffer.data)) {
                if (buffer.offset - Protocol.PACKAGE_DELIM.size != Protocol.DATA_RECEIVE_SIZE) {
                    System.err.println("data packet has wrong size")
                } else {
                    val dataReceive = Protocol.deserializeData(buffer.data)
                    onDeviceData?.invoke(deviceName, dataReceive)
                }
            } else if (Protocol.memoryCompare("DEBUG".toByteArray(), buffer.data)) {
                val debugReceive =
                    String(buffer.data, 5, buffer.offset - 5 - Protocol.PACKAGE_DELIM.size)
                onDeviceDebug?.invoke(deviceName, debugReceive)
            } else if (buffer.data[0] == '{'.code.toByte()) {
                try{
                    // glove information sent as modified json without quotes
                    val information =
                        String(buffer.data, 0, buffer.offset - Protocol.PACKAGE_DELIM.size)
                    // unstrip double quotes to create valid json again
                    val json = information.replace(Regex("[\\w]+")) {
                        "\"${it.value}\""
                    }

                    val deserialized = JsonHelper.fromJson<InformationV1In>(json)
                    onDeviceInformation?.invoke(deviceName, deserialized)
                }
                catch (ex:Exception){
                    onDeviceError?.invoke(deviceName,ex.toString());
                    requestInformation()
                }
            }

        }
    }

    public fun requestInformation() {
        val data = DataSend(requestInformation = true)
        dataSendQueue.add(data)
    }

    public fun sendData(data: DataSend) {
        dataSendQueue.add(data)
    }

    private fun sendDataImmediately(data: DataSend) {
        // throttle data sending
        val now = System.currentTimeMillis()
        if (now - writeTimestamp < 10) {
            onDeviceError?.invoke(deviceName, "Dropped command due to throttling")
            return
        }
        writeTimestamp = now
        data.serialize(packageSendBuffer)
        try {
            synchronized(writeLock) {
                serial.write(packageSendBuffer, 500)
                serial.write(Protocol.PACKAGE_DELIM, 500)

            }
        } catch (e: IOException) {
            onDeviceError?.invoke(deviceName, "Error writing data: ${e.message}")
        }
    }


    private fun readRoutine() {
        val buffer = ByteArray(512)
        var numBytesRead: Int
        while (!Thread.currentThread().isInterrupted) {
            try {
                try {
                    numBytesRead = serial.read(buffer, 1000)
                    for (i in 0 until numBytesRead) {
                        readPackage(buffer[i], packageReadBuffer)
                    }
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
                while (!dataSendQueue.isEmpty()) {
                    sendDataImmediately(dataSendQueue.remove())
                }
                Thread.sleep(5)
            } catch (e: InterruptedException) {
                onDeviceError?.invoke(deviceName, "Write routine interrupted: ${e.message}")
                return
            }
        }
    }
}