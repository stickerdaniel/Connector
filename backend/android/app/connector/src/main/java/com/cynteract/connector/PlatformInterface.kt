package com.cynteract.connector

import kotlinx.serialization.Serializable

// this file is the same as in the Unity frontend
// nullable is not used as the "required" keyword is missing in Unity (C# 9)


@Serializable
data class Dataframe(
    val force: ShortArray,
    val imu: Array<IMUData>,
    val imuStatus: ByteArray,
    val vibStatus: ByteArray
) {
    @Serializable
    data class IMUData(
        val x: Float,
        val y: Float,
        val z: Float,
        val w: Float
    )
}

@Serializable
data class DeviceCommand(
    val vibration: ByteArray,
    val vibrationPattern: ByteArray
)

@Serializable
data class Information(
    val Hand: String,
    //Version field was missing in the first firmware
    val version: String = "1"
)

@Serializable
open class Message(val type: String) {
    @Serializable
    data class Scan(val connectionType: String) : Message("scan")

    @Serializable
    data class Connect(
        val deviceId: String,
        val connectionType: String,
        val isConnected: Boolean,
        val version: String,
        val information: Information
    ) : Message("connect")

    @Serializable
    data class Disconnect(val deviceId: String) : Message("disconnect")

    @Serializable
    data class Data(val deviceId: String, val data: Dataframe) : Message("data")

    @Serializable
    data class Command(val deviceId: String, val command: DeviceCommand) : Message("command")

    @Serializable
    data class Debug(val deviceId: String, val message: String) : Message("debug")

    @Serializable
    data class Error(val deviceId: String, val message: String) : Message("error")

    fun write(writer: (String) -> Unit) {
        val json = JsonHelper.toJson(this)
        synchronized(syncLock) {
            writer(json)
        }
    }

    companion object {
        private val syncLock = Any()


        fun readLine(line: String): Message {
            return fromJson(line)
        }

        fun fromJson(json: String): Message {
            val baseMessage = JsonHelper.fromJson<Message>(json)

            return when (baseMessage.type) {
                "scan" -> JsonHelper.fromJson<Scan>(json)
                "connect" -> JsonHelper.fromJson<Connect>(json)
                "disconnect" -> JsonHelper.fromJson<Disconnect>(json)
                "data" -> JsonHelper.fromJson<Data>(json)
                "command" -> JsonHelper.fromJson<Command>(json)
                "debug" -> JsonHelper.fromJson<Debug>(json)
                "error" -> JsonHelper.fromJson<Error>(json)
                else -> throw Exception("Unknown message type: ${baseMessage.type}")
            }
        }
    }
}
