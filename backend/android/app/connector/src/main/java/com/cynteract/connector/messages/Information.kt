package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Information(
    val deviceType: String,
    val firmwareVersion: String,
    val firmwareDate: String,
    val checkpoint: String,
    val vibrationPositions: Array<String>,
    val imuPositions: Array<String>
)
