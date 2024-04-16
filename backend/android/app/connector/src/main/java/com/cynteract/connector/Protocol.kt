package com.cynteract.connector

import java.nio.ByteBuffer
import java.nio.ByteOrder

interface Protocol {

    companion object {
        val PACKAGE_DELIM = "CYNTERACT\n".toByteArray()
        const val DATA_SEND_SIZE = DataSend.SIZE
        const val DATA_RECEIVE_SIZE = DataReceive.SIZE

        fun memoryCompare(sequence: ByteArray, array: ByteArray, offset: Int = 0): Boolean {
            for (i in sequence.indices) {
                if (sequence[i] != array[offset + i]) {
                    return false
                }
            }
            return true
        }

        fun deserializeData(bytes: ByteArray): DataReceive {
            val data = DataReceive()
            data.deserialize(bytes)
            return data
        }
    }

}

data class IMUData(
    var x: Float,
    var y: Float,
    var z: Float,
    var w: Float
)

data class DataReceive(
    var header: ByteArray = "DATA".toByteArray(),
    var force: ShortArray = ShortArray(8),
    var imu: Array<IMUData> = Array(16) { IMUData(0f, 0f, 0f, 0f) },
    var imuStatus: ByteArray = ByteArray(16),
    var vibStatus: ByteArray = ByteArray(10)
) {
    companion object {
        const val SIZE = 4 + 8 * 2 + 16 * 4 * 4 + 16 + 10 + 2
    }

    fun serialize(bytes: ByteArray) {
        val bb = ByteBuffer.wrap(bytes)
        bb.order(ByteOrder.BIG_ENDIAN) // or LITTLE_ENDIAN
        bb.put(header)
        for (i in 0 until 8) {
            bb.putShort(force[i])
        }
        for (i in 0 until 16) {
            bb.putFloat(imu[i].x)
            bb.putFloat(imu[i].y)
            bb.putFloat(imu[i].z)
            bb.putFloat(imu[i].w)
        }
        bb.put(imuStatus)
        bb.put(vibStatus)
        //conform to c struct padding
        bb.putShort(0.toShort())
    }

    fun deserialize(bytes: ByteArray) {
        val bb = ByteBuffer.wrap(bytes)
        bb.order(ByteOrder.LITTLE_ENDIAN) // or LITTLE_ENDIAN
        bb.get(header)
        for (i in 0 until 8) {
            force[i] = bb.getShort()
        }
        for (i in 0 until 16) {
            imu[i].x = bb.getFloat()
            imu[i].y = bb.getFloat()
            imu[i].z = bb.getFloat()
            imu[i].w = bb.getFloat()
        }
        bb.get(imuStatus)
        bb.get(vibStatus)
    }
}

data class DataSend(
    val header: ByteArray = "DATA".toByteArray(),
    val vibration: ByteArray = ByteArray(10),
    val vibrationPattern: ByteArray = ByteArray(10),
    var requestInformation: Boolean = false
) {
    companion object {
        const val SIZE = 4 + 10 + 10 + 1
    }

    fun serialize(bytes: ByteArray) {
        val bb = ByteBuffer.wrap(bytes)
        bb.order(ByteOrder.BIG_ENDIAN)
        bb.put(header)
        bb.put(vibration)
        bb.put(vibrationPattern)
        bb.put(if (requestInformation) 1 else 0)
    }

    fun deserialize(bytes: ByteArray) {
        val bb = ByteBuffer.wrap(bytes)
        bb.order(ByteOrder.BIG_ENDIAN)
        bb.get(header)
        bb.get(vibration)
        bb.get(vibrationPattern)
        requestInformation = bb.get() == 1.toByte()
    }
}


internal class Packet(bytes: ByteArray?) {
    private val type: Int
    private val data1: Float
    private val data2: Short

    init {
        val bb = ByteBuffer.wrap(bytes)
        bb.order(ByteOrder.BIG_ENDIAN) // or LITTLE_ENDIAN
        type = bb.getInt()
        data1 = bb.getFloat()
        data2 = bb.getShort()
    }
}