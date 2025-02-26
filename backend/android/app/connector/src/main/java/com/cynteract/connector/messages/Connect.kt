package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Connect(
    val connectionType: String,
)
