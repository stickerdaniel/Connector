package com.cynteract.connector.usb

import android.app.PendingIntent
import android.app.PendingIntent.FLAG_IMMUTABLE
import android.app.PendingIntent.FLAG_UPDATE_CURRENT
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbManager
import android.util.Log
import androidx.core.content.ContextCompat
import com.cynteract.connector.ACTION_USB_DEVICE_ATTACHED
import com.cynteract.connector.ACTION_USB_DEVICE_DETACHED
import com.cynteract.connector.ACTION_USB_PERMISSION
import com.cynteract.connector.HardwareInterface

import com.hoho.android.usbserial.driver.UsbSerialProber
import java.io.IOException

class UsbSerialDevice(
    val androidUsbDevice: UsbDevice,
    val usbDevice: com.cynteract.connector.usb.UsbDevice
)

class UsbReceiver : BroadcastReceiver(), HardwareInterface {
    private var closed: Boolean = false
    override val connectionType: String = "usb"
    private lateinit var manager: UsbManager
    private lateinit var permissionIntent: PendingIntent

    private var deviceMap = mapOf<String, UsbSerialDevice>()

    override var onDeviceConnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceError: ((HardwareInterface, String, String) -> Unit)? = null
    override var onDeviceMessage: ((HardwareInterface, String, Any) -> Unit)? = null

    fun init(context: Context) {
        manager = context.getSystemService(Context.USB_SERVICE) as UsbManager


        //Create a filter for the actions, the usb receiver should listen to
        val filter = IntentFilter()
        filter.addAction(ACTION_USB_PERMISSION)
        filter.addAction(ACTION_USB_DEVICE_ATTACHED)
        filter.addAction(ACTION_USB_DEVICE_DETACHED)

        ContextCompat.registerReceiver(
            context, this, filter,
            ContextCompat.RECEIVER_EXPORTED
        )

        updateDevices(context)
    }

    private fun updateDevices(context: Context) {
        scanForDevices(context)
        askPermissions(context)
    }

    private fun scanForDevices(context: Context) {
        val devices = manager.deviceList
        //Add new devices to the map
        devices.forEach { device ->
            if (!deviceMap.containsKey(device.key)) {
                val drivers = UsbSerialProber.getDefaultProber().findAllDrivers(manager)
                val driver = UsbSerialProber.getDefaultProber().probeDevice(device.value)
                val usbDevice = UsbDevice(
                    deviceName = device.key,
                    serial = driver.ports[0],
                    openConnection = fun() {
                        val connection: UsbDeviceConnection = manager.openDevice(driver.device)
                            ?: throw IOException("Cannot open device")
                        driver.ports[0].open(connection)
                    }
                )
                usbDevice.onDeviceConnected = ::deviceOnDeviceConnected
                usbDevice.onDeviceError = ::deviceOnDeviceError
                usbDevice.onDeviceMessage = ::deviceOnDeviceMessage
                deviceMap += Pair(
                    device.key,
                    UsbSerialDevice(
                        device.value,
                        usbDevice
                    )
                )
            }
        }
        //Close removed devices
        deviceMap.keys.forEach { device ->
            if (!devices.keys.contains(device)) {
                val serialDevice = deviceMap[device]!!.usbDevice
                serialDevice.close()
                onDeviceDisconnected?.invoke(this, device)
            }
        }
        //Remove removed devices from the map
        deviceMap = deviceMap.filter { entry -> devices.keys.contains(entry.key) }
    }

    private fun askPermissions(context: Context) {
        deviceMap.values.forEach { device ->
            permissionIntent = PendingIntent.getBroadcast(
                context, 0,
                Intent(ACTION_USB_PERMISSION),
                FLAG_UPDATE_CURRENT or FLAG_IMMUTABLE
            )
            manager.requestPermission(device.androidUsbDevice, permissionIntent)
        }
    }

    private fun startDevices() {
        deviceMap.forEach { action ->
            if (manager.hasPermission(action.value.androidUsbDevice)) {
                if (!action.value.usbDevice.running) {
                    Log.d("UsbReceiver", "Device ${action.key} has permission")
                    action.value.usbDevice.start()
                }
            }
        }
    }

    override fun onReceive(context: Context?, intent: Intent) {
        synchronized(this) {
            if (closed) return
            when (intent.action) {
                ACTION_USB_PERMISSION -> {
                    Log.d("UsbReceiver", "Permission")
                    startDevices()
                }

                ACTION_USB_DEVICE_ATTACHED -> {
                    Log.d("UsbReceiver", "Device attached")
                    updateDevices(context!!)
                }

                ACTION_USB_DEVICE_DETACHED -> {
                    Log.d("UsbReceiver", "Device detached")
                    updateDevices(context!!)
                }
            }
        }
    }

    private fun deviceOnDeviceConnected(id: String) {
        onDeviceConnected?.invoke(this, id)
    }

    private fun deviceOnDeviceError(id: String, error: String) {
        onDeviceError?.invoke(this, id, error)
    }

    private fun deviceOnDeviceMessage(id: String, message: Any) {
        onDeviceMessage?.invoke(this, id, message)
    }

    override fun sendMessage(id: String, data: Any) {
        deviceMap[id]?.usbDevice?.sendMessage(data)
    }

    override fun close(context: Context) {
        synchronized(this) {
            closed = true
            deviceMap.keys.forEach { device ->
                val serialDevice = deviceMap[device]!!.usbDevice
                serialDevice.close()
            }
        }
    }
}
