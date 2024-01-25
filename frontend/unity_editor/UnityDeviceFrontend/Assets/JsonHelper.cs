
// common interface for System.Text.Json and UnityEngine.JsonUtility

using UnityEngine;

public class JsonHelper
{
    public static string ToJson(object obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public static T FromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }
}