package com.cynteract.connector.messages

import com.cynteract.connector.JsonHelper
import kotlin.reflect.KClass
import kotlin.reflect.jvm.jvmName

object Factory {
    private val messageTypes = mutableMapOf<String, KClass<*>>()
    private val jsonSerializers = mutableMapOf<String, (Any) -> String>()
    private val jsonDeserializers = mutableMapOf<String, (String) -> Any>()

    init {
        registerMessageType<Command>("command")
        registerMessageType<Connect>("connect")
        registerMessageType<Dataframe>("dataframe")
        registerMessageType<Debug>("debug")
        registerMessageType<Disconnect>("disconnect")
        registerMessageType<Error>("error")
        registerMessageType<Information>("information")
        registerMessageType<InformationRequest>("informationRequest")
        registerMessageType<Scan>("scan")
        registerMessageType<Touchtex>("touchtex")
    }

    private inline fun <reified T> registerMessageType(type: String) {
        if (messageTypes.containsKey(type)) {
            throw IllegalStateException("Message type already registered: $type")
        }
        messageTypes[type] = T::class
        jsonSerializers[type] = { JsonHelper.toJson(it as T) }
        jsonDeserializers[type] = { JsonHelper.fromJson<T>(it) as Any }
    }

    fun getMessageTypeName(message: Any): String {
        return messageTypes.entries.find { it.value == message::class }?.key
            ?: throw IllegalArgumentException("Unknown message type: ${message::class.jvmName}")
    }

    fun getMessageType(type: String): KClass<*> {
        return messageTypes[type] ?: throw IllegalArgumentException("Unknown message type: $type")
    }

    fun toJson(message: Any): String {
        val messageTypeName = getMessageTypeName(message)
        val serializer = jsonSerializers[messageTypeName]!!
        return serializer(message)
    }

    fun fromJson(json: String, type: String): Any {
        val deserializer = jsonDeserializers[type]
            ?: throw IllegalArgumentException("Unknown message type: $type")
        return deserializer(json)
    }
}
