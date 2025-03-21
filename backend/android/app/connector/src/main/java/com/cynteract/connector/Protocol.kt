package com.cynteract.connector

import com.cynteract.connector.messages.Factory
import kotlinx.serialization.Serializable


@Serializable
private data class MessageHeader(val deviceId: String?, val type: String)

object Protocol {
    fun serialize(deviceId: String?, message: Any): String {
        val messageTypeName = Factory.getMessageTypeName(message)
        val header = MessageHeader(deviceId, messageTypeName)
        val headerContent = JsonHelper.toJson(header).removeSurrounding("{", "}")
        val bodyContent = Factory.toJson(message).removeSurrounding("{", "}")
        if (bodyContent.isEmpty())
            return "{ $headerContent }"
        else
            return "{ $headerContent, $bodyContent }"
    }

    fun deserialize(json: String): Pair<String?, Any> {
        val header = JsonHelper.fromJson<MessageHeader>(json)
        val message = Factory.fromJson(json, header.type)
        return Pair(header.deviceId, message)
    }
}
