using System.Text.Json;

public class TestData
{
    public static List<Dictionary<string, string>> Data { get; private set; }
    static TestData()
    {
        Data = new List<Dictionary<string, string>>();
        string json = File.ReadAllText("TestData/testdata.json");
        JsonDocument doc = JsonDocument.Parse(json);
        foreach (JsonElement element in doc.RootElement.EnumerateArray())
        {
            var entry = new Dictionary<string, string>();
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                    entry.Add(property.Name, property.Value.GetString()!);
                else
                    entry.Add(property.Name, property.Value.GetRawText());
            }
            Data.Add(entry);
        }
    }
    public static string PrettyPrintByteArray(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    public static byte[] ParsePrettyPrintedByteArray(string data)
    {
        string[] byteStrings = data.Split(' ');
        byte[] byteArray = new byte[byteStrings.Length];
        for (int i = 0; i < byteStrings.Length; i++)
        {
            byteArray[i] = Convert.ToByte(byteStrings[i], 16);
        }
        return byteArray;
    }

    public static void AssertJsonEquals(string expected, string actual)
    {
        string expectedNormalized = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(expected));
        string actualNormalized = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(actual));
        Assert.That(actualNormalized, Is.EqualTo(expectedNormalized),
            $"JSONs are not equal:\njson1: {expectedNormalized}\njson2: {actualNormalized}");
    }
}