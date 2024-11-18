package com.cynteract.connector

import kotlinx.serialization.Serializable

// this file is taken from the C# part

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
data class InformationV1In(
    val Hand: String,
    val version: String = "",
    val Vibration: Map<String, String>,
    val IMU: Map<String, String>
)

@Serializable
data class InformationV1Out(
    var hand: String,
    var version: String,
    var vibration: List<String>,
    var imu: List<String>
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
        val information: InformationV1Out
    ) : Message("connect")

    @Serializable
    data class Disconnect(val deviceId: String) : Message("disconnect")

    @Serializable
    data class Data(val deviceId: String, val data: Dataframe) : Message("data")

    @Serializable
    data class Command(val deviceId: String, val command: DeviceCommand) : Message("command")


    @Serializable
    data class InformationRequest(val deviceId: String) : Message("informationRequest")
    @Serializable
    data class Debug(val deviceId: String, val message: String) : Message("debug")

    @Serializable
    data class Error(val deviceId: String, val message: String) : Message("error")

    fun write(writer: (String) -> Unit) {
        // smart cast so that toJson will serialize all fields from the subclass
        val json = when (this) {
            is Scan -> JsonHelper.toJson(this)
            is Connect -> JsonHelper.toJson(this)
            is Disconnect -> JsonHelper.toJson(this)
            is Data -> JsonHelper.toJson(this)
            is Command -> JsonHelper.toJson(this)
            is Debug -> JsonHelper.toJson(this)
            is Error -> JsonHelper.toJson(this)
            else -> throw Exception("Unknown message type: ${this.type}")
        }
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
                "informationRequest"->JsonHelper.fromJson<InformationRequest>(json)
                "data" -> JsonHelper.fromJson<Data>(json)
                "command" -> JsonHelper.fromJson<Command>(json)
                "debug" -> JsonHelper.fromJson<Debug>(json)
                "error" -> JsonHelper.fromJson<Error>(json)
                else -> throw Exception("Unknown message type: ${baseMessage.type}")
            }
        }
    }
}
