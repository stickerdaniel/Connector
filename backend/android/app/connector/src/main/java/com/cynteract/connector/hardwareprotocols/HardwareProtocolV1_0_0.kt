package com.cynteract.connector.hardwareprotocols

import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets

/** This version adds back the double quotes to the information json package. */
@Suppress("ClassName")
class HardwareProtocolV1_0_0 : HardwareProtocolVersion {
    override val version: String = "1.0.0"
    private val protocolV090 = HardwareProtocolV0_9_0()

    override fun serialize(writer: ByteBuffer, message: Any) {
        protocolV090.serialize(writer, message)
    }

    override fun deserialize(reader: ByteBuffer): Any {
        val buffer = ByteArray(reader.remaining()).apply { reader.get(this) }

        if (String(buffer, StandardCharsets.UTF_8).startsWith("{\"Hand\"")) {
            val information = String(buffer, StandardCharsets.UTF_8)
            // Strip the double quotes from the information json package.
            val informationV090 = information.replace(Regex("\"([\\w]+)\""), "$1")
            val bufferV090 = informationV090.toByteArray(StandardCharsets.UTF_8)
            val readerV090 = ByteBuffer.wrap(bufferV090).order(ByteOrder.LITTLE_ENDIAN)
            return protocolV090.deserialize(readerV090)
        } else {
            reader.position(0)
            return protocolV090.deserialize(reader)
        }
    }
}