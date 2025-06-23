package com.cynteract.connector.hardwareprotocols

import com.cynteract.connector.messages.Factory
import com.cynteract.connector.messages.Information
import kotlinx.serialization.Serializable
import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import kotlin.reflect.KClass
import kotlin.reflect.full.memberProperties
import kotlin.reflect.full.primaryConstructor
import kotlin.reflect.jvm.isAccessible

@Suppress("ClassName")
class HardwareProtocolV2_0_0 : HardwareProtocolVersion {
    override val version: String = "2.0.0"

    override fun serialize(writer: ByteBuffer, message: Any) {
        serializeValue(writer, version)
        serializeValue(writer, Factory.getMessageTypeName(message))
        serializeValue(writer, message)
    }

    override fun deserialize(reader: ByteBuffer): Any {
        val protocolVersion = deserializeValue(reader, String::class) as String
        if (protocolVersion != version) {
            throw IllegalArgumentException("Protocol version mismatch. Expected $version, got $protocolVersion")
        }
        
        val messageTypeName = deserializeValue(reader, String::class) as String
        val messageType = when (messageTypeName) {
            "information" -> InformationV2_0_0::class
            else -> Factory.getMessageType(messageTypeName)
        }
        // Transform to current message format.
        val transformedMessage = when (val message = deserializeValue(reader, messageType)) {
            is InformationV2_0_0 -> Information(
                deviceType = message.deviceType,
                hardwareVersion = "not implemented",
                firmwareVersion = message.firmwareVersion,
                firmwareDate = message.firmwareDate,
                // users were all therapists before home version was introduced
                userType = "pro",
                checkpoint = message.checkpoint,
                vibrationPositions = message.vibrationPositions,
                imuPositions = message.imuPositions
            )

            else -> message
        }

        return transformedMessage
    }

    fun serializeValue(buffer: ByteBuffer, value: Any) {
        when (value) {
            is String -> {
                val bytes = value.toByteArray(StandardCharsets.UTF_8)
                buffer.put(bytes)
                buffer.put(0)
            }

            is Byte -> buffer.put(value)
            is ByteArray -> {
                buffer.put(value.size.toByte())
                value.forEach { buffer.put(it) }
            }

            is Short -> buffer.putShort(value)
            is ShortArray -> {
                buffer.put(value.size.toByte())
                value.forEach { buffer.putShort(it) }
            }

            is Int -> buffer.putInt(value)
            is IntArray -> {
                buffer.put(value.size.toByte())
                value.forEach { buffer.putInt(it) }
            }

            is Float -> buffer.putFloat(value)
            is Array<*> -> {
                buffer.put(value.size.toByte())
                value.forEach { serializeValue(buffer, it!!) }
            }

            else -> {
                val propertyNames = value::class.primaryConstructor!!.parameters.map { it.name }
                propertyNames.forEach { propertyName ->
                    val field = value::class.memberProperties.first { it.name == propertyName }
                    field.isAccessible = true
                    serializeValue(buffer, field.getter.call(value)!!)
                }
            }
        }
    }

    fun deserializeValue(buffer: ByteBuffer, type: KClass<*>): Any {
        return when (type) {
            String::class -> {
                val bytes = ByteArrayOutputStream()
                var byte: Byte
                do {
                    byte = buffer.get()
                    if (byte != 0.toByte()) {
                        bytes.write(byte.toInt())
                    }
                } while (byte != 0.toByte())
                bytes.toString(StandardCharsets.UTF_8.name())
            }

            Byte::class -> buffer.get()
            ByteArray::class -> {
                val size = buffer.get().toInt()
                val array = ByteArray(size) { buffer.get() }
                array
            }

            Short::class -> buffer.short
            ShortArray::class -> {
                val size = buffer.get().toInt()
                val array = ShortArray(size) { buffer.short }
                array
            }

            Int::class -> buffer.int
            IntArray::class -> {
                val size = buffer.get().toInt()
                val array = IntArray(size) { buffer.int }
                array
            }

            Float::class -> buffer.float
            else -> {
                if (type.java.isArray) {
                    val componentType = type.java.componentType.kotlin
                    val size = buffer.get().toInt()
                    val array =
                        java.lang.reflect.Array.newInstance(componentType.java, size) as Array<Any>
                    for (i in 0 until size) {
                        array[i] = deserializeValue(buffer, componentType)
                    }
                    array
                } else {
                    val propertyValues =
                        type.primaryConstructor!!.parameters.associateWith { parameter ->
                            val fieldType = parameter.type.classifier as KClass<*>
                            deserializeValue(buffer, fieldType)
                        }
                    val instance = type.primaryConstructor!!.callBy(propertyValues)
                    instance
                }
            }
        }
    }

    @Serializable
    class InformationV2_0_0(
        val deviceType: String,
        val firmwareVersion: String,
        val firmwareDate: String,
        val checkpoint: String,
        val vibrationPositions: Array<String>,
        val imuPositions: Array<String>
    )
}
