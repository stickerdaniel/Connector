package com.cynteract.connector

import android.app.PendingIntent
import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.hardware.usb.UsbManager
import androidx.core.content.ContextCompat
import com.hoho.android.usbserial.driver.UsbSerialDriver
import com.hoho.android.usbserial.driver.UsbSerialProber
import java.util.concurrent.ConcurrentLinkedQueue

// ...

class Usb : BroadcastReceiver(), HardwareInterface {

    private lateinit var usbManager: UsbManager


    class PackageReadBuffer {
        var transmissionStartTime: Long = 0
        var data = ByteArray(1024)
        var offset = 0
    }

    private var device: SerialDevice? = null

    private val scanLock=Any()

    override var onDeviceConnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceDisconnected: ((HardwareInterface, String) -> Unit)? = null
    override var onDeviceError: ((HardwareInterface, String, String) -> Unit)? = null
    override var onDeviceInformation: ((HardwareInterface, String, InformationV1In) -> Unit)? = null
    override var onDeviceData: ((HardwareInterface, String, DataReceive) -> Unit)? = null
    override var onDeviceDebug: ((HardwareInterface, String, String) -> Unit)? = null

    private val serviceThread: Thread= Thread { serviceRoutine() }
    private val serviceQueue: ConcurrentLinkedQueue<(()->Unit)> = ConcurrentLinkedQueue()

    override fun requestInformation(id: String) {
        device?.requestInformation()
    }

    override fun sendData(id: String, data: DataSend) {
        device?.sendData(data)
    }


    override val connectionType: String = ConnectionType.Usb


    fun init(context: Context) {
        //LocalBroadcastManager.getInstance(context)
        ContextCompat.registerReceiver(
            context, this, IntentFilter(ACTION_USB_PERMISSION),
            ContextCompat.RECEIVER_EXPORTED
        )
        // register Receiver for USB events
        context.registerReceiver(
            this,
            IntentFilter("android.hardware.usb.action.USB_STATE")
        )

        usbManager = context.getSystemService(Context.USB_SERVICE) as UsbManager
        serviceThread.start()
        serviceQueue.add { startScan(context) }
    }


    override fun startScan(context: Context) {
        synchronized(scanLock){
        val availableDrivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager)
        var driver: UsbSerialDriver? = null
        // request permission for usb device
        if (availableDrivers.isNotEmpty()) {
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
            val device = SerialDevice(
                driver=driver,
                serial = driver.ports[0],
                usbManager=usbManager
            )
            device.onDeviceData=::deviceOnDeviceData
            device.onDeviceConnected=::deviceOnDeviceConnected
            device.onDeviceError=::deviceOnDeviceError
            device.onDeviceInformation=::deviceOnDeviceInformation
            device.onDeviceDebug=::deviceOnDeviceDebug
            device.onRequestConnectionCheck=::deviceOnDeviceRequestConnectionCheck
            this.device = device
        }

        // device disconnected
        if (device != null && driver == null) {
            stopDevice()
            }
        }
    }

    private fun  recheckConnection(){
        val availableDrivers =
            UsbSerialProber.getDefaultProber().findAllDrivers(usbManager)
        if (availableDrivers.isEmpty()) {
            stopDevice()
        }
    }





    private fun serviceRoutine(){
        while (true){
            while (serviceQueue.isNotEmpty()){
                serviceQueue.remove().invoke()
            }
            Thread.sleep(50)
        }
    }

    private fun startDevice(device: SerialDevice) {
        device.start()
    }

    // safely disconnect device
    private fun stopDevice() {
        onDeviceDisconnected?.invoke(this, "USB")
        device?.close()
        this.device = null
    }





    companion object {
        const val ACTION_USB_PERMISSION = "com.cynteract.connector.USB_PERMISSION"
    }

    private fun  deviceOnDeviceData (id:String,dataReceive: DataReceive){
        onDeviceData?.invoke(this,id,dataReceive)
    }
    private fun deviceOnDeviceConnected(id: String) {
        onDeviceConnected?.invoke(this,id)
    }
    private fun deviceOnDeviceError(id: String, error: String) {
        onDeviceError?.invoke(this,id,error)
    }
    private fun deviceOnDeviceInformation(id: String, information: InformationV1In) {
        onDeviceInformation?.invoke(this,id,information)
    }
    private fun deviceOnDeviceDebug(id: String, message: String) {
        onDeviceDebug?.invoke(this,id,message)
    }
    private fun deviceOnDeviceRequestConnectionCheck(id: String) {
        onDeviceDebug?.invoke(this,id,"Requesting connection check")
        serviceQueue.add { recheckConnection() }
    }
    override fun onReceive(context: Context?, intent: Intent?) {
        val action = intent?.action
        if(!this::usbManager.isInitialized){
            usbManager = context!!.getSystemService(Context.USB_SERVICE) as UsbManager
        }
        if ("android.hardware.usb.action.USB_STATE" == action) {
            if (intent.extras?.getBoolean("connected", false) == true) {
                // connected
                serviceQueue.add { startScan(context!!) }

            } else {
                // disconnected
                serviceQueue.add { startScan(context!!) }
            }
        } else if (ACTION_USB_PERMISSION == action) {
            val granted =
                usbManager.hasPermission(device?.driver?.device)//intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)

            if (granted) {
                device?.let { startDevice(it) }
            }
        }
    }
}

