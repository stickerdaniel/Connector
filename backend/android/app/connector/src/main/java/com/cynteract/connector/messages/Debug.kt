package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Debug(
    val message: String,
)