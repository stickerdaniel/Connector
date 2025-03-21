package com.cynteract.connector

import android.util.Log

/** Logger interface to allow for different logging implementations */
interface Logger {
    fun d(tag: String, message: String)
}

/** Default implementation of Logger using android.util.Log */
class AndroidLogger : Logger {
    override fun d(tag: String, message: String) {
        Log.d(tag, message)
    }
}

/** Object to wrap Logger, mockable for testing */
object Log {
    private var instance: Logger = AndroidLogger()

    fun setLogger(logger: Logger) {
        instance = logger
    }

    fun d(tag: String, message: String) {
        instance.d(tag, message)
    }
}