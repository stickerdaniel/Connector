package com.cynteract.connector

import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbManager
import android.util.Log
import androidx.core.content.ContextCompat
import com.hoho.android.usbserial.driver.UsbSerialDriver
import com.hoho.android.usbserial.driver.UsbSerialPort
import com.hoho.android.usbserial.driver.UsbSerialProber
import java.io.IOException

// ...

class Usb : BroadcastReceiver(), HardwareInterface {

    private lateinit var usbManager: UsbManager

    class SerialDevice {
        lateinit var driver: UsbSerialDriver
        lateinit var serial: UsbSerialPort
        lateinit var readThread: Thread
        val packageReadBuffer = PackageReadBuffer()
        val packageSendBuffer = ByteArray(Protocol.DATA_SEND_SIZE)
        val writeLock = Any()
        // throttle sending data
        var writeTimestamp = 0L
    }

    class PackageReadBuffer {
        var transmissionStartTime: Long = 0
        var data = ByteArray(1024)
        var offset = 0
    }

    var device: SerialDevice? = null


    override var onDeviceConnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceError: ((HardwareInterface, String, String) -> Unit)? = null
    override var onDeviceInformation: ((HardwareInterface, String, Information) -> Unit)? = null
    override var onDeviceData: ((HardwareInterface, String, DataReceive) -> Unit)? = null
    override var onDeviceDebug: ((HardwareInterface, String, String) -> Unit)? = null


    override val connectionType: String = "USB"


    fun init(context: Context) {

        //LocalBroadcastManager.getInstance(context)
        ContextCompat.registerReceiver(
            context, this, IntentFilter(ACTION_USB_PERMISSION),
            ContextCompat.RECEIVER_NOT_EXPORTED
        )
        // register Receiver for USB events
        context.registerReceiver(
            this,
            IntentFilter("android.hardware.usb.action.USB_STATE")
        )

        usbManager = context.getSystemService(Context.USB_SERVICE) as UsbManager

        startScan(context)
    }


    override fun startScan(context: Context) {
        val availableDrivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager)
        var driver: UsbSerialDriver? = null
        // request permission for usb device
        if (!availableDrivers.isEmpty()) {
            driver = availableDrivers[0]
            val permissionIntent = PendingIntent.getBroadcast(
                context,
                0,
                Intent(ACTION_USB_PERMISSION),
                PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
            )
            usbManager.requestPermission(driver.device, permissionIntent)
        }

        // new device found
        if (device == null && driver != null) {
            //initialize device
            val device = SerialDevice()
            device.driver = driver
            device.serial = driver.ports[0]
            device.readThread = Thread { readRoutine() }
            this.device = device
        }

        // device disconnected
        if (device != null && driver == null) {
            stopDevice()
        }
    }


    private fun startDevice(device: SerialDevice) {
        Log.d("USB", "Starting device")
        try {
            val connection: UsbDeviceConnection = usbManager.openDevice(device.driver.device)
                ?: throw IOException("Cannot open device")
            device.serial.open(connection)
            device.serial.setParameters(
                230400,
                8,
                UsbSerialPort.STOPBITS_1,
                UsbSerialPort.PARITY_NONE
            )
            onDeviceConnected?.invoke(this, "USB")
            device.readThread.start()

        } catch (e: IOException) {
            onDeviceError?.invoke(this, "USB", "Error opening device: ${e.message}")
        }

    }

    // safely disconnect device
    private fun stopDevice() {
        val device: SerialDevice = device ?: return
        onDeviceDisconnected?.invoke(this, "USB")
        try {
            device.serial.close()
        } catch (e: IOException) {
        }
        device.readThread.interrupt()
        this.device = null
    }

    private fun readRoutine() {
        val device = this.device!!
        val buffer = ByteArray(512)
        var numBytesRead: Int
        while (!Thread.currentThread().isInterrupted) {
            try {
                synchronized(device.writeLock) {
                    try {
                        numBytesRead = device.serial.read(buffer, 1000)
                        for (i in 0 until numBytesRead) {
                            readPackage("USB", buffer[i], device.packageReadBuffer)
                        }
                    } catch (e: IOException) {
                        onDeviceError?.invoke(this, "USB", "Error reading device: ${e.message}")
                        // check if the device is still connected
                        val availableDrivers =
                            UsbSerialProber.getDefaultProber().findAllDrivers(usbManager)
                        if (availableDrivers.isEmpty()) {
                            stopDevice()
                            // exit read thread
                            return
                        } else {
                            // wait longer, probably the disconnect event will kick in
                            Thread.sleep(1000)
                        }
                    }
                }
                Thread.sleep(5)
            } catch (e: InterruptedException) {
            }
        }
    }

    fun readPackage(id: String, x: Byte, buffer: PackageReadBuffer) {

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
                    onDeviceData?.invoke(this, id, dataReceive)
                }
            } else if (Protocol.memoryCompare("DEBUG".toByteArray(), buffer.data)) {
                val debugReceive =
                    String(buffer.data, 5, buffer.offset - 5 - Protocol.PACKAGE_DELIM.size)
                onDeviceDebug?.invoke(this, id, debugReceive)
            } else if (buffer.data[0] == '{'.code.toByte()) {
                // glove information sent as modified json without quotes
                val information =
                    String(buffer.data, 0, buffer.offset - Protocol.PACKAGE_DELIM.size)
                // unstrip double quotes to create valid json again
                val json = information.replace(Regex("[\\w]+")) {
                    "\"${it.value}\""
                }

                val deserialized = JsonHelper.fromJson<Information>(json)
                onDeviceInformation?.invoke(this, id, deserialized)
            }

        }
    }

    override fun requestInformation(id: String) {
        val data = DataSend(requestInformation = true)
        sendData(id, data)
    }

    override fun sendData(id: String, data: DataSend) {
        val device: SerialDevice = device ?: return
        // throttle data sending
        val now = System.currentTimeMillis()
        if (now - device.writeTimestamp < 10) {
            onDeviceError?.invoke(this, "USB", "Dropped command due to throttling")
            return
        }
        device.writeTimestamp = now
        data.serialize(device.packageSendBuffer)
        try {
            synchronized(device.writeLock) {
                device.serial.write(device.packageSendBuffer, 500)
                device.serial.write(Protocol.PACKAGE_DELIM, 500)

            }
        } catch (e: IOException) {
            onDeviceError?.invoke(this, "USB", "Error writing data: ${e.message}")
        }
    }

    override fun onReceive(context: Context?, intent: Intent?) {
        val action = intent?.action
        if ("android.hardware.usb.action.USB_STATE" == action) {
            if (intent.extras?.getBoolean("connected", false) == true) {
                // connected
                startScan(context!!)
            } else {
                // disconnected
                startScan(context!!)
            }
        } else if (ACTION_USB_PERMISSION == action) {
            val granted =
                usbManager.hasPermission(device?.driver?.device)//intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)

            if (granted) {
                device?.let { startDevice(it) }
            }
        }
    }

    companion object {
        const val ACTION_USB_PERMISSION = "com.cynteract.connector.USB_PERMISSION"
    }
}

