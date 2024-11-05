package com.cynteract.connector

import android.app.PendingIntent
import android.app.PendingIntent.FLAG_IMMUTABLE
import android.app.PendingIntent.FLAG_UPDATE_CURRENT
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.UsbDevice
import android.hardware.usb.UsbManager
import android.util.Log
import androidx.core.content.ContextCompat
import com.hoho.android.usbserial.driver.UsbSerialProber

class UsbSerialDevice(
    public val usbDevice: UsbDevice,
    public val serialDevice: SerialDevice
) {

}

class UsbReceiver : BroadcastReceiver(), HardwareInterface {
    override val connectionType: String="USB"
    private lateinit var manager: UsbManager
    private lateinit var permissionIntent: PendingIntent

    private var deviceMap = mapOf<String, UsbSerialDevice>()

    override var onDeviceConnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceError: ((HardwareInterface, String, String) -> Unit)? = null
    override var onDeviceInformation: ((HardwareInterface, String, InformationV1In) -> Unit)? = null
    override var onDeviceData: ((HardwareInterface, String, DataReceive) -> Unit)? = null
    override var onDeviceDebug: ((HardwareInterface, String, String) -> Unit)? = null

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
                val drivers=UsbSerialProber.getDefaultProber().findAllDrivers(manager)
                val driver = UsbSerialProber.getDefaultProber().probeDevice(device.value)
                val serialDevice = SerialDevice(
                    deviceName = device.key,
                    driver = driver,
                    serial =  driver.ports[0],
                    usbManager =  manager
                )
                serialDevice.onDeviceData = ::deviceOnDeviceData
                serialDevice.onDeviceConnected = ::deviceOnDeviceConnected
                serialDevice.onDeviceError = ::deviceOnDeviceError
                serialDevice.onDeviceInformation = ::deviceOnDeviceInformation
                serialDevice.onDeviceDebug = ::deviceOnDeviceDebug
                serialDevice.onRequestConnectionCheck = ::deviceOnDeviceRequestConnectionCheck
                deviceMap += Pair(
                    device.key,
                    UsbSerialDevice(
                        device.value,
                        serialDevice
                        )
                )
            }
        }
        //Close removed devices
        deviceMap.keys.forEach { device ->
            if (!devices.keys.contains(device)) {
                val serialDevice=deviceMap[device]!!.serialDevice
                serialDevice.close()
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
            manager.requestPermission(device.usbDevice, permissionIntent)
        }
    }

    private fun startDevices() {
        deviceMap.forEach { action ->
            if (manager.hasPermission(action.value.usbDevice)) {
                if(!action.value.serialDevice.running) {
                    Log.d("UsbReceiver", "Device ${action.key} has permission")
                    action.value.serialDevice.start()
                }
            }
        }
    }

    override fun onReceive(context: Context?, intent: Intent) {
        synchronized(this){
            when (intent.action) {
                ACTION_USB_PERMISSION -> {
                    Log.d("UsbReceiver", "Permission")
                    startDevices();
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
    private fun deviceOnDeviceData(id: String, dataReceive: DataReceive) {
        onDeviceData?.invoke(this, id, dataReceive)
    }

    private fun deviceOnDeviceConnected(id: String) {
        onDeviceConnected?.invoke(this, id)
    }

    private fun deviceOnDeviceError(id: String, error: String) {
        onDeviceError?.invoke(this, id, error)
    }

    private fun deviceOnDeviceInformation(id: String, information: InformationV1In) {
        onDeviceInformation?.invoke(this, id, information)
    }

    private fun deviceOnDeviceDebug(id: String, message: String) {
        onDeviceDebug?.invoke(this, id, message)
    }

    private fun deviceOnDeviceRequestConnectionCheck(id: String) {
        onDeviceDebug?.invoke(this, id, "Requesting connection check")

    }

    override fun requestInformation(id: String) {
        deviceMap[id]?.serialDevice?.requestInformation()
    }

    override fun sendData(id: String, data: DataSend) {
        deviceMap[id]?.serialDevice?.sendData(data)
    }





}