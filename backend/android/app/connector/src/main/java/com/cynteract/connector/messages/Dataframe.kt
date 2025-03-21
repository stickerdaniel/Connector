package com.cynteract.connector.messages

import kotlinx.serialization.Serializable

@Serializable
class IMUData(val x: Float, val y: Float, val z: Float, val w: Float)

@Serializable
class Dataframe @OptIn(ExperimentalUnsignedTypes::class) constructor(
    val forceValues: ShortArray,
    val imuValues: Array<IMUData>,
    val imuStates: UByteArray,
    val vibrationStates: UByteArray
)

