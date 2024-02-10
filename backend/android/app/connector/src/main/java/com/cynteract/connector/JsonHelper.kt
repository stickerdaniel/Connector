package com.cynteract.connector

import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

object JsonHelper {

    // ToJson function
    inline fun <reified T> toJson(obj: T): String {
        // val json = Json(JsonConfiguration.Default.copy(classDiscriminator = "className"))

        //val config = JsonConfiguration.Default.copy(classDiscriminator = "className")

        return Json.encodeToString(obj)
    }

    // FromJson function
    inline fun <reified T> fromJson(json: String): T {
        return Json.decodeFromString(json)
    }
}
