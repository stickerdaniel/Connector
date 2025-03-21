package com.cynteract.connector

import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.jsonArray
import kotlinx.serialization.json.jsonObject
import org.junit.jupiter.api.Assertions.assertEquals
import java.io.File

object TestData {
    val data: MutableList<Map<String, String>> = mutableListOf()

    init {
        val json = File("src/test/resources/testdata.json").readText()
        val doc = Json.parseToJsonElement(json)
        for (element in doc.jsonArray) {
            val entry = mutableMapOf<String, String>()
            for ((key, value) in element.jsonObject.entries) {
                entry[key] = if (value is JsonPrimitive && value.isString) {
                    value.content
                } else {
                    value.toString()
                }
            }
            data.add(entry)
        }
    }

    fun prettyPrintByteArray(data: ByteArray): String {
        return data.joinToString(" ") { String.format("%02X", it) }
    }

    fun parsePrettyPrintedByteArray(data: String): ByteArray {
        return data.split(" ").map { it.toInt(16).toByte() }.toByteArray()
    }

    fun assertJsonEquals(expected: String, actual: String) {
        val normalizedExpected = Json.encodeToString(Json.decodeFromString<JsonElement>(expected))
        val normalizedActual = Json.encodeToString(Json.decodeFromString<JsonElement>(actual))
        assertEquals(
            normalizedExpected,
            normalizedActual,
            "JSONs are not equal:\nexpected: $normalizedExpected\nactual: $normalizedActual",
        )
    }
}