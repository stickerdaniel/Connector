package com.cynteract.connector

/** Object to print timing related debug log */
object Timer {
    private var startTime = 0L

    fun log(label: String, reset: Boolean = false) {
        if (startTime == 0L || reset) {
            startTime = System.currentTimeMillis()
        }
        Log.d("Timer", "$label: +${System.currentTimeMillis() - startTime} ms")
    }

    private var spanStartTime = 0L
    private var spanLabel = ""
    fun span(label: String, action: () -> Unit) {
        spanStartTime = System.currentTimeMillis()
        action()
        Log.d("Timer", "$label: ${System.currentTimeMillis() - spanStartTime} ms")
    }

    fun spanStart(label: String) {
        spanEnd()
        spanStartTime = System.currentTimeMillis()
        spanLabel = label
    }

    fun spanEnd() {
        if (spanStartTime == 0L) return
        Log.d("Timer", "$spanLabel: ${System.currentTimeMillis() - spanStartTime} ms")
        spanStartTime = 0L
    }
}