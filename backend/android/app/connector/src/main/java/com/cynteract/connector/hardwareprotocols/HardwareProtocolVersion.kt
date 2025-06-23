package com.cynteract.connector.hardwareprotocols

import java.nio.ByteBuffer

interface HardwareProtocolVersion {
    val version: String
    fun serialize(writer: ByteBuffer, message: Any)
    fun deserialize(reader: ByteBuffer): Any
}