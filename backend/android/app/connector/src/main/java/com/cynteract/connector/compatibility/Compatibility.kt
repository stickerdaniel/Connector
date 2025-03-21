package com.cynteract.connector.compatibility

import java.nio.ByteBuffer

interface Compatibility {
    val version: String
    fun serialize(writer: ByteBuffer, message: Any)
    fun deserialize(reader: ByteBuffer): Any
}