
// common interface for System.Text.Json and UnityEngine.JsonUtility

using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonHelper
{
    public static string ToJson(object obj)
    {
        var options = new JsonSerializerOptions
        {
            // PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        return JsonSerializer.Serialize(obj, options);
    }

    public static T FromJson<T>(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
        };
        return JsonSerializer.Deserialize<T>(json, options);
    }
}