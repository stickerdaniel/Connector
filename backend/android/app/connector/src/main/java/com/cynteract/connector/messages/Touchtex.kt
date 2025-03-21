package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class TouchtexPart @OptIn(ExperimentalUnsignedTypes::class) constructor(
    var vibrations: UByteArray,
    var heat: Byte
)

@Serializable
class Touchtex @OptIn(ExperimentalUnsignedTypes::class) constructor(
    var mainVibration: Byte,
    var fingerVibrations: UByteArray,
    var touchTexBoards: Array<TouchtexPart>,
    var frontPressure: Byte,
    var backPressure: Byte,
    var handPressure: Byte
)
