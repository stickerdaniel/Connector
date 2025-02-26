package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class Touchtex(
    var vibrationMode: String,
    var mainVibration: Int,
    var fingerVibrations: IntArray,
    var armVibrations: IntArray,
    var heat: Byte
)
