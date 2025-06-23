package com.cynteract.connector

import com.cynteract.connector.hardwareprotocols.HardwareProtocolV1_0_0
import com.cynteract.connector.hardwareprotocols.HardwareProtocolV2_0_0
import com.cynteract.connector.hardwareprotocols.HardwareProtocolV2_1_0
import com.cynteract.connector.hardwareprotocols.HardwareProtocolVersion
import java.nio.ByteBuffer
import java.nio.charset.StandardCharsets

class HardwareProtocol {
    private val PROTOCOL_VERSION = "2.1.0"
    private val protocol: HardwareProtocolVersion = HardwareProtocolV2_1_0()
    private var compatibility: HardwareProtocolVersion? = null

    fun serialize(buffer: ByteBuffer, message: Any) {
        if (compatibility == null) {
            protocol.serialize(buffer, message)
        } else {
            compatibility!!.serialize(buffer, message)
        }
    }

    fun deserialize(buffer: ByteBuffer): Any {
        val tryProtocols = listOf(
            HardwareProtocolV2_1_0(),
            HardwareProtocolV2_0_0(),
            HardwareProtocolV1_0_0()
        )

        for (protocol in tryProtocols) {
            try {
                buffer.position(0)
                val message = protocol.deserialize(buffer)
                compatibility = protocol
                return message
            } catch (e: Exception) {
                // Ignore compatibility exceptions and try the next protocol.
            }
        }

        // Get start of message for reporting.
        buffer.position(0)
        val head = ByteArray(10)
        buffer.get(head, 0, head.size)
        val headHex = head.joinToString(" ") { String.format("%02X", it) }
        val headString = String(head, StandardCharsets.UTF_8)
        val errorMessage =
            "Failed to deserialize message.\nFirst 10 bytes: [$headHex]\nDecoded: '$headString'."
        throw IllegalArgumentException(errorMessage)
    }
}
