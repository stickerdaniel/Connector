package com.cynteract.connector.compatibility

import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets

/** This version adds back the double quotes to the information json package. */
class CompatibilityV1_0_0 : Compatibility {
    override val version: String = "1.0.0"

    override fun serialize(writer: ByteBuffer, message: Any) {
        compatibilityV0_9.serialize(writer, message)
    }

    override fun deserialize(reader: ByteBuffer): Any {
        val buffer = ByteArray(reader.remaining()).apply { reader.get(this) }

        if (String(buffer, StandardCharsets.UTF_8).startsWith("{\"Hand\"")) {
            val information = String(buffer, StandardCharsets.UTF_8)
            // Strip the double quotes from the information json package.
            val informationV0_9_0 = information.replace(Regex("\"([\\w]+)\""), "$1")
            val bufferV0_9_0 = informationV0_9_0.toByteArray(StandardCharsets.UTF_8)
            val readerV0_9_0 = ByteBuffer.wrap(bufferV0_9_0).order(ByteOrder.LITTLE_ENDIAN)
            return compatibilityV0_9.deserialize(readerV0_9_0)
        } else {
            reader.position(0)
            return compatibilityV0_9.deserialize(reader)
        }
    }

    private val compatibilityV0_9 = CompatibilityV0_9_0()
}