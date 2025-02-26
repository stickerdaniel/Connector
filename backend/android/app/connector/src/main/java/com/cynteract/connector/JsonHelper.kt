package com.cynteract.connector


import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

object JsonHelper {
    val json =
        Json {
            ignoreUnknownKeys = true; classDiscriminator = ""
        }

    inline fun <reified T> toJson(obj: T): String {
        return json.encodeToString(obj)
    }

    inline fun <reified T> fromJson(jsonString: String): T {
        return json.decodeFromString(jsonString)
    }

}