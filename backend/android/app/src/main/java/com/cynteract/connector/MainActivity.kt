package com.cynteract.connector

import android.app.Activity
import android.os.Bundle
import android.util.Log
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class MainActivity : Activity() {

    private lateinit var txtTerminal: TextView
    private lateinit var edtMessage: EditText
    private var plugin = Main(object : MessageListener {
        override fun onMessage(message: String) {
            Log.d("Main", "onMessage: $message")
        }
    })

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)

        txtTerminal = findViewById(R.id.txtTerminal)
        edtMessage = findViewById(R.id.edtMessage)
        val btnSend = findViewById<Button>(R.id.btnSend)
        plugin.initialize(this)
    }


    private fun sendMessage() {
        val message = edtMessage.text.toString()
        if (message.isNotEmpty()) {
            val timestamp = SimpleDateFormat("HH:mm:ss", Locale.getDefault()).format(Date())
            appendMessage("TX [$timestamp]: $message", R.color.tint_blue)

            // Simulating RX response (dummy message)
            val rxMessage = "RX [$timestamp]: hi \n"
            appendMessage(rxMessage, R.color.tint_red)

            edtMessage.text.clear()
        }
    }

    private fun appendMessage(message: String, colorResId: Int) {
        val timestamp = SimpleDateFormat("HH:mm:ss", Locale.getDefault()).format(Date())
        val formattedMessage = "$message\n"
        val coloredMessage = "<font color='${getColor(colorResId)}'>$formattedMessage</font><br>"

        txtTerminal.append(
            android.text.Html.fromHtml(
                coloredMessage,
                android.text.Html.FROM_HTML_MODE_LEGACY
            )
        )
        scrollToBottom()
    }

    private fun scrollToBottom() {
        txtTerminal.post {
            val scrollAmount = txtTerminal.lineHeight * txtTerminal.lineCount - txtTerminal.height
            if (scrollAmount > 0) {
                txtTerminal.scrollTo(0, scrollAmount)
            } else {
                txtTerminal.scrollTo(0, 0)
            }
        }
    }
}
