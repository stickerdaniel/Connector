package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Command(
    val vibrationValues: ByteArray,
    val vibrationPatterns: ByteArray
)
