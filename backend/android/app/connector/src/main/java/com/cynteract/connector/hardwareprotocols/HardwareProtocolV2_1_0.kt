package com.cynteract.connector.hardwareprotocols

import com.cynteract.connector.messages.Factory
import java.nio.ByteBuffer

@Suppress("ClassName")
class HardwareProtocolV2_1_0 : HardwareProtocolVersion {
    override val version: String = "2.1.0"
    private val protocolV200 = HardwareProtocolV2_0_0()

    override fun serialize(writer: ByteBuffer, message: Any) {
        protocolV200.serializeValue(writer, version)
        protocolV200.serializeValue(writer, Factory.getMessageTypeName(message))
        protocolV200.serializeValue(writer, message)
    }

    override fun deserialize(reader: ByteBuffer): Any {
        val protocolVersion = protocolV200.deserializeValue(reader, String::class) as String
        if (protocolVersion != version) {
            throw IllegalArgumentException("Protocol version mismatch. Expected $version, got $protocolVersion")
        }

        val messageTypeName = protocolV200.deserializeValue(reader, String::class) as String
        val messageType = Factory.getMessageType(messageTypeName)
        val message = protocolV200.deserializeValue(reader, messageType)

        return message
    }
}
