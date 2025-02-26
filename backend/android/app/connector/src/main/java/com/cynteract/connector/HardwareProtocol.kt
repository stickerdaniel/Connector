package com.cynteract.connector

import com.cynteract.connector.compatibility.Compatibility
import com.cynteract.connector.compatibility.CompatibilityV1_0_0
import com.cynteract.connector.messages.Factory
import java.io.ByteArrayOutputStream
import java.nio.BufferUnderflowException
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets
import kotlin.reflect.KClass
import kotlin.reflect.full.memberProperties
import kotlin.reflect.full.primaryConstructor
import kotlin.reflect.jvm.isAccessible

class HardwareProtocol {
    private val PROTOCOL_VERSION = "2.0.0"
    private var compatibility: Compatibility? = null

    fun serialize(buffer: ByteBuffer, message: Any) {
        if (compatibility == null) {
            serializeValue(buffer, PROTOCOL_VERSION)
            serializeValue(buffer, Factory.getMessageTypeName(message))
            serializeValue(buffer, message)
        } else {
            compatibility!!.serialize(buffer, message)
        }
    }

    private fun serializeValue(buffer: ByteBuffer, value: Any) {
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

    fun deserialize(buffer: ByteBuffer): Any {
        try {
            val protocolVersion = deserializeValue(buffer, String::class) as String
            if (protocolVersion != PROTOCOL_VERSION) {
                throw IllegalArgumentException("Protocol version mismatch. Expected $PROTOCOL_VERSION, got $protocolVersion")
            }
            val messageTypeName = deserializeValue(buffer, String::class) as String
            val messageType = Factory.getMessageType(messageTypeName)
            return deserializeValue(buffer, messageType)
        } catch (e: Exception) {
            when (e) {
                is IllegalArgumentException, is BufferUnderflowException -> {
                    try {
                        val compatibilityV1 = CompatibilityV1_0_0()
                        buffer.position(0)
                        val message = compatibilityV1.deserialize(buffer)
                        compatibility = compatibilityV1
                        return message
                    } catch (e2: IllegalArgumentException) {
                        // Get start of message for reporting.
                        buffer.position(0)
                        val head = ByteArray(10)
                        buffer.get(head, 0, head.size)
                        val headHex = head.joinToString(" ") { String.format("%02X", it) }
                        val headString = String(head, StandardCharsets.UTF_8)
                        var errorMessage =
                            "Failed to deserialize message.\nFirst 10 bytes: [$headHex]\nDecoded: '$headString'."
                        errorMessage += "\nOriginal exception: '${e.message ?: e.javaClass.name}'"
                        errorMessage += "\nCompatibility exception: '${e2.message ?: e2.javaClass.name}'"
                        throw IllegalArgumentException(errorMessage)
                    }
                }

                else -> throw e
            }
        }
    }

    private fun deserializeValue(buffer: ByteBuffer, type: KClass<*>): Any {
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
}
