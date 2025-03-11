package com.cynteract.connector

import android.hardware.usb.UsbDeviceConnection
import android.hardware.usb.UsbEndpoint
import com.cynteract.connector.usb.UsbDevice
import com.hoho.android.usbserial.driver.UsbSerialDriver
import com.hoho.android.usbserial.driver.UsbSerialPort
import org.junit.jupiter.api.AfterEach
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.fail
import org.junit.jupiter.api.BeforeAll
import org.junit.jupiter.api.BeforeEach
import org.junit.jupiter.api.Test
import java.util.EnumSet
import java.util.concurrent.LinkedBlockingQueue
import java.util.concurrent.TimeUnit

//@Timeout(value = 500, unit = TimeUnit.MILLISECONDS)
class UsbDeviceTest {

    private lateinit var usbDevice: UsbDevice
    private lateinit var serialPortMock: UsbSerialPortMock
    private val errorQueue = LinkedBlockingQueue<Pair<String, String>>()
    private val messageQueue = LinkedBlockingQueue<Pair<String, Any>>()

    companion object {
        @BeforeAll
        @JvmStatic
        fun setupLogger() {
            Log.setLogger(object : Logger {
                override fun d(tag: String, message: String) {
                    println("DEBUG: [$tag] $message")
                }
            })
        }
    }

    @BeforeEach
    fun startUsbDevice() {
        serialPortMock = UsbSerialPortMock()
        usbDevice = UsbDevice(
            deviceName = "COM1",
            serial = serialPortMock,
            openConnection = fun() {}
        ).apply {
            onDeviceError = { portName, errorMessage ->
                Log.d("UsbDeviceTest", "Device error: $errorMessage")
                errorQueue.put(portName to errorMessage)
            }
            onDeviceMessage = { portName, message ->
                messageQueue.put(portName to message)
            }
        }
        usbDevice.start()
    }

    @AfterEach
    fun stopUsbDevice() {
        usbDevice.close()
        assert(!serialPortMock.isOpen) { "Serial port should be closed" }
    }

    @Test
    fun testThreadsStop() {
        usbDevice.close()
    }

    @Test
    fun testReceiveMessage() {
        val (json1, binary1) = getTestEntry()
        serialPortMock.feedDelimited(binary1)
        assertReceivedMessage(json1)
        assert(messageQueue.isEmpty())
    }

    @Test
    fun testMultiple() {
        val (json1, binary1) = getTestEntry()
        val count = 1000
        repeat(count) { serialPortMock.feedDelimited(binary1) }
        repeat(count) { assertReceivedMessage(json1) }
        assert(messageQueue.isEmpty())
    }

    @Test
    fun testBrokenMessages() {
        val (json1, binary1) = getTestEntry()

        // correct message
        serialPortMock.feedDelimited(binary1)
        assertReceivedMessage(json1)

        // corrupt message
        serialPortMock.feedDelimited("broken stuff".toByteArray())
        assertReceivedError()
        serialPortMock.feedDelimited(binary1)
        assertReceivedMessage(json1)

        // buffer overflow
        serialPortMock.feed(ByteArray(usbDevice.bufferSize() + 10))
        assertReceivedError()
        serialPortMock.feedDelimited(byteArrayOf())
        assertReceivedError()
        serialPortMock.feedDelimited(binary1)
        assertReceivedMessage(json1)

        // timeout
        serialPortMock.feed(ByteArray(10))
        Thread.sleep(usbDevice.packageTimeout() + 10)
        serialPortMock.feedDelimited(binary1)
        assertReceivedError()
        assertReceivedMessage(json1)

        assertEquals(0, messageQueue.size)
    }

    private fun assertReceivedMessage(expected: String) {
        val nameAndMessage = messageQueue.poll(500, TimeUnit.MILLISECONDS)
            ?: fail("No message received.")
        assertEquals("COM1", nameAndMessage.first)
        val json = Protocol.serialize("testDevice", nameAndMessage.second)
        TestData.assertJsonEquals(expected, json)
    }

    private fun assertReceivedError() {
        val nameAndMessage = errorQueue.poll(500, TimeUnit.MILLISECONDS)
            ?: fail("No error received.")
        assert(errorQueue.isEmpty()) {
            val nameAndMessage2 = errorQueue.poll()!!
            fail("More than one error received. First error: ${nameAndMessage.second}, Second error: ${nameAndMessage2.second}")
        }
    }

    private fun getTestEntry(): Pair<String, ByteArray> {
        val testEntry = TestData.data.first { it["name"] == "debug" }
        val json1 = testEntry["protocol"] as String
        val binary1 = TestData.parsePrettyPrintedByteArray(testEntry["hardwareProtocol"] as String)
        return json1 to binary1
    }


    class UsbSerialPortMock : UsbSerialPort {
        private val dataStream = LinkedBlockingQueue<Byte>()
        private var isOpen = false
        fun feed(data: ByteArray) {
            data.forEach { dataStream.put(it) }
        }

        fun feedDelimited(data: ByteArray) {
            feed(data + UsbDevice.PACKAGE_DELIM)
        }

        override fun open(connection: UsbDeviceConnection?) {
            isOpen = true
        }

        override fun close() {
            isOpen = false
        }

        override fun isOpen(): Boolean {
            return isOpen
        }

        override fun setParameters(baudRate: Int, dataBits: Int, stopBits: Int, parity: Int) {}

        override fun read(dest: ByteArray?, timeout: Int): Int {
            var bytesRead = 0
            // wait for data to arrive
            if (dataStream.isEmpty()) {
                val firstByte = dataStream.poll(timeout.toLong(), TimeUnit.MILLISECONDS)
                if (firstByte != null) {
                    dest!![bytesRead++] = firstByte
                }
            }
            // read all available data
            while (bytesRead < dest!!.size && dataStream.isNotEmpty()) {
                dest[bytesRead++] = dataStream.take()
            }
            return bytesRead
        }


        override fun getDriver(): UsbSerialDriver {
            TODO("Not yet implemented")
        }

        override fun getDevice(): android.hardware.usb.UsbDevice {
            TODO("Not yet implemented")
        }

        override fun getPortNumber(): Int {
            TODO("Not yet implemented")
        }

        override fun getWriteEndpoint(): UsbEndpoint {
            TODO("Not yet implemented")
        }

        override fun getReadEndpoint(): UsbEndpoint {
            TODO("Not yet implemented")
        }

        override fun getSerial(): String {
            TODO("Not yet implemented")
        }

        override fun write(src: ByteArray?, timeout: Int) {
            TODO("Not yet implemented")
        }

        override fun getCD(): Boolean {
            TODO("Not yet implemented")
        }

        override fun getCTS(): Boolean {
            TODO("Not yet implemented")
        }

        override fun getDSR(): Boolean {
            TODO("Not yet implemented")
        }

        override fun getDTR(): Boolean {
            TODO("Not yet implemented")
        }

        override fun setDTR(value: Boolean) {
            TODO("Not yet implemented")
        }

        override fun getRI(): Boolean {
            TODO("Not yet implemented")
        }

        override fun getRTS(): Boolean {
            TODO("Not yet implemented")
        }

        override fun setRTS(value: Boolean) {
            TODO("Not yet implemented")
        }

        override fun getControlLines(): EnumSet<UsbSerialPort.ControlLine> {
            TODO("Not yet implemented")
        }

        override fun getSupportedControlLines(): EnumSet<UsbSerialPort.ControlLine> {
            TODO("Not yet implemented")
        }

        override fun purgeHwBuffers(purgeWriteBuffers: Boolean, purgeReadBuffers: Boolean) {
            TODO("Not yet implemented")
        }

        override fun setBreak(value: Boolean) {
            TODO("Not yet implemented")
        }
    }
}