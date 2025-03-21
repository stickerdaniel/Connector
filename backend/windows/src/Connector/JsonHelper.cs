using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonHelper
{
    private static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Converters = { new ByteArrayToIntArrayConverter() }
        };
    }

    public static string ToJson(object obj)
    {
        var options = GetSerializerOptions();
        return JsonSerializer.Serialize(obj, options);
    }

    public static T FromJson<T>(string json)
    {
        var options = GetSerializerOptions();
        return JsonSerializer.Deserialize<T>(json, options);
    }

    public static object FromJson(string json, Type type)
    {
        var options = GetSerializerOptions();
        return JsonSerializer.Deserialize(json, type, options);
    }
}

public class ByteArrayToIntArrayConverter : JsonConverter<byte[]>
{
    public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var intArray = JsonSerializer.Deserialize<int[]>(ref reader, options);
        return Array.ConvertAll(intArray, item => (byte)item);
    }

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
    {
        var intArray = Array.ConvertAll(value, item => (int)item);
        JsonSerializer.Serialize(writer, intArray, options);
    }
}
