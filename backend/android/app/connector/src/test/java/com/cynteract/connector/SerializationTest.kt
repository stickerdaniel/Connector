import com.cynteract.connector.HardwareProtocol
import com.cynteract.connector.Protocol
import com.cynteract.connector.TestData
import com.cynteract.connector.hardwareprotocols.HardwareProtocolV0_9_0
import com.cynteract.connector.hardwareprotocols.HardwareProtocolV1_0_0
import com.cynteract.connector.hardwareprotocols.HardwareProtocolV2_0_0
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.Arguments
import org.junit.jupiter.params.provider.MethodSource
import java.nio.ByteBuffer

class SerializationTest {
    private fun prettyPrintBuffer(buffer: ByteBuffer): String {
        val bytes = ByteArray(buffer.remaining())
        buffer.get(bytes)
        buffer.position(0)
        return TestData.prettyPrintByteArray(bytes)
    }

    @ParameterizedTest
    @MethodSource("loadTestData")
    fun serializeTest(name: String, protocolJson: String, hardwareProtocolBinaryPretty: String) {
        val hardwareProtocol = HardwareProtocol()
        val (deviceId, message1) = Protocol.deserialize(protocolJson)
        val binary1 = ByteBuffer.allocate(512)
        hardwareProtocol.serialize(binary1, message1)
        binary1.flip()
        val binary1Pretty = prettyPrintBuffer(binary1)
        assertEquals(hardwareProtocolBinaryPretty, binary1Pretty)
        val message2 = hardwareProtocol.deserialize(binary1)
        val json2 = Protocol.serialize(deviceId, message2)
        TestData.assertJsonEquals(protocolJson, json2)
    }

    @ParameterizedTest
    @MethodSource("loadCompatibilityTestData")
    fun serializeCompatibilityTest(
        name: String,
        protocolJson: String,
        hardwareProtocolBinaryPretty: String,
        compatibilityVersion: String,
        direction: String
    ) {
        val compatibility = when (compatibilityVersion) {
            "0.9.0" -> HardwareProtocolV0_9_0()
            "1.0.0" -> HardwareProtocolV1_0_0()
            "2.0.0" -> HardwareProtocolV2_0_0()
            else -> throw NotImplementedError("Unknown compatibility version: $compatibilityVersion")
        }
        if (direction == "toDevice") {
            val (deviceId, message1) = Protocol.deserialize(protocolJson)
            val binary1 = ByteBuffer.allocate(512)
            compatibility.serialize(binary1, message1)
            binary1.flip()
            val binary1Pretty = prettyPrintBuffer(binary1)
            assertEquals(hardwareProtocolBinaryPretty, binary1Pretty)
        } else if (direction == "fromDevice") {
            val binary1 = ByteBuffer.wrap(
                hardwareProtocolBinaryPretty.split(" ").map { it.toInt(16).toByte() }.toByteArray()
            )
            val message1 = HardwareProtocol().deserialize(binary1)
            val json2 = Protocol.serialize("testDevice", message1)
            TestData.assertJsonEquals(protocolJson, json2)
        }
    }

    companion object {
        @JvmStatic
        fun loadTestData(): List<Arguments> = TestData.data.filter {
            !it.contains("compatibility")
        }.map {
            Arguments.of(it.get("name"), it.get("protocol"), it.get("hardwareProtocol"))
        }

        @JvmStatic
        fun loadCompatibilityTestData(): List<Arguments> = TestData.data.filter {
            it.contains("compatibility")
        }.map {
            Arguments.of(
                it.get("name"),
                it.get("protocol"),
                it.get("hardwareProtocol"),
                it.get("compatibility"),
                it.get("direction")
            )
        }
    }
}
