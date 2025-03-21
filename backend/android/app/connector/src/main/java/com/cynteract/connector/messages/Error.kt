package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Error(
    val message: String
)
