package com.cynteract.connector.compatibility

import com.cynteract.connector.JsonHelper
import com.cynteract.connector.messages.Command
import com.cynteract.connector.messages.Dataframe
import com.cynteract.connector.messages.Debug
import com.cynteract.connector.messages.IMUData
import com.cynteract.connector.messages.Information
import com.cynteract.connector.messages.InformationRequest
import kotlinx.serialization.Serializable
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets

class CompatibilityV0_9_0 : Compatibility {
    override val version: String = "0.9.0"

    override fun serialize(writer: ByteBuffer, message: Any) {
        val transformedMessage = when (message) {
            is InformationRequest -> CommandV0_9().apply { requestInformation = true }
            is Command -> CommandV0_9().apply {
                vibration = message.vibrationValues
                vibrationPattern = message.vibrationPatterns
            }

            else -> throw NotImplementedError("Missing compatibility for message: ${message::class.java.simpleName}")
        }

        writer.put(transformedMessage.toByteArray())
    }

    override fun deserialize(reader: ByteBuffer): Any {
        val buffer = ByteArray(reader.remaining()).apply { reader.get(this) }

        val message: Any = when {
            String(buffer, StandardCharsets.UTF_8).startsWith("DEBUG") -> Debug(
                message = String(buffer.drop(5).toByteArray(), StandardCharsets.UTF_8)
            )

            String(buffer, StandardCharsets.UTF_8).startsWith("DATA") -> {
                val dataframe = DataframeV0_9.fromByteArray(buffer)
                dataframe
            }

            String(buffer, StandardCharsets.UTF_8).startsWith("{Hand") -> {
                val information = String(buffer, StandardCharsets.UTF_8)
                // Re-add double quotes to the json. The double quotes are removed for a shorter package size.
                val json = Regex("""[\w]+""").replace(information) { "\"${it.value}\"" }
                JsonHelper.fromJson<InformationV0_9>(json)
            }

            else -> throw IllegalArgumentException("Unknown message format.")
        }

        return when (message) {
            is InformationV0_9 -> Information(
                deviceType = when (message.Hand) {
                    "Rechts" -> "rightGlove"
                    "Links" -> "leftGlove"
                    "Cushion" -> "cushion"
                    "Strap" -> "strap"
                    else -> message.Hand
                },
                checkpoint = "not implemented",
                firmwareVersion = "not implemented",
                firmwareDate = "not implemented",
                vibrationPositions = message.Vibration.values.toTypedArray(),
                imuPositions = message.IMU.values.toTypedArray()
            )

            is DataframeV0_9 -> Dataframe(
                forceValues = message.force,
                imuValues = message.imu.map { imuData ->
                    IMUData(
                        x = imuData.x,
                        y = imuData.y,
                        z = imuData.z,
                        w = imuData.w
                    )
                }.toTypedArray(),
                imuStates = message.imuStatus,
                vibrationStates = message.vibStatus
            )

            is Debug -> message
            else -> throw IllegalArgumentException("Unknown message format.")
        }
    }

    @Serializable
    private data class InformationV0_9(
        val Hand: String,
        val Vibration: Map<String, String>,
        val IMU: Map<String, String>
    )

    private data class DataframeV0_9 @OptIn(ExperimentalUnsignedTypes::class) constructor(
        val header: ByteArray,
        val force: ShortArray,
        val imu: Array<IMUData>,
        val imuStatus: UByteArray,
        val vibStatus: UByteArray
    ) {
        companion object {
            @OptIn(ExperimentalUnsignedTypes::class)
            fun fromByteArray(bytes: ByteArray): DataframeV0_9 {
                val buffer = ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN)
                val header = ByteArray(4).apply { buffer.get(this) }
                val force = ShortArray(8).apply { for (i in indices) this[i] = buffer.short }
                val imu =
                    Array(16) { IMUData(buffer.float, buffer.float, buffer.float, buffer.float) }
                val imuStatus = ByteArray(16).apply { buffer.get(this) }.toUByteArray()
                val vibStatus = ByteArray(10).apply { buffer.get(this) }.toUByteArray()
                return DataframeV0_9(header, force, imu, imuStatus, vibStatus)
            }
        }

        data class IMUData(val x: Float, val y: Float, val z: Float, val w: Float)
    }

    private data class CommandV0_9(
        val header: ByteArray = byteArrayOf(
            'D'.code.toByte(), 'A'.code.toByte(), 'T'.code.toByte(),
            'A'.code.toByte()
        ),
        var vibration: ByteArray = ByteArray(10),
        var vibrationPattern: ByteArray = ByteArray(10),
        var requestInformation: Boolean = false
    ) {
        companion object {
            const val SIZE = 4 + 10 + 10 + 1
        }

        fun toByteArray(): ByteArray {
            val buffer = ByteBuffer.allocate(SIZE).order(ByteOrder.LITTLE_ENDIAN)
            buffer.put(header)
            buffer.put(vibration)
            buffer.put(vibrationPattern)
            buffer.put(if (requestInformation) 1 else 0)
            return buffer.array()
        }
    }
}