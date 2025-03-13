using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;


public class TestData
{
    public static List<Dictionary<string, string>> Data { get; private set; }
    const string TestDataPath = "../../backend/windows/tests/Connector.Tests/TestData/testdata.json";
    static TestData()
    {
        Data = new List<Dictionary<string, string>>();
        string json = File.ReadAllText(TestDataPath);
        JArray doc = JArray.Parse(json);
        foreach (JObject element in doc)
        {
            var entry = new Dictionary<string, string>();
            foreach (var property in element.Properties())
            {
                entry.Add(property.Name, property.Value.ToString());
            }
            Data.Add(entry);
        }
    }

    public static void AssertJsonEquals(string expected, string actual)
    {
        JObject expectedNormalized = JObject.Parse(expected);
        JObject actualNormalized = JObject.Parse(actual);

        // 1.0 and 0.999999 should be considered equal
        RoundFloats(expectedNormalized, 1e-6);
        RoundFloats(actualNormalized, 1e-6);

        Assert.That(actualNormalized.ToString(), Is.EqualTo(expectedNormalized.ToString()),
            $"JSONs are not equal:\njson1: {expectedNormalized}\njson2: {actualNormalized}");
    }

    private static void RoundFloats(JToken token, double epsilon)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                RoundFloats(property.Value, epsilon);
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
            {
                RoundFloats(item, epsilon);
            }
        }
        else if (token is JValue value && value.Type == JTokenType.Float)
        {
            value.Value = Math.Round(value.Value<double>(), (int)Math.Log10(1 / epsilon));
        }
    }
}