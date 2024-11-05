package com.cynteract.connector

class PackageReadBuffer {
    var transmissionStartTime: Long = 0
    var data = ByteArray(1024)
    var offset = 0
}