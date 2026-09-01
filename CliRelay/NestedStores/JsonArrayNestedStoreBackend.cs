using System.Text.Json;
using System.Text.Json.Nodes;
using HsMan.NestedStore;

namespace CliRelay.NestedStores;

public class JsonArrayNestedStoreBackend(JsonArray jsonArray) : INestedStoreBackend
{
    private static int ParseIndex(string key)
    {
        return int.TryParse(key, out var index) 
            ? index 
            : throw new ArgumentException($"Key '{key}' is not a valid index number");
    }

    private bool IsIndexValid(int index)
    {
        return index >= 0 && index < jsonArray.Count;
    }

    private int ValidateIndex(string key)
    {
        var index = ParseIndex(key);
        return IsIndexValid(index) 
            ? index 
            : throw new IndexOutOfRangeException($"Index '{index}' is out of range for key '{key}'");
    }
    
    public void SetValue(string key, object? value)
    {
        var index = ValidateIndex(key);
        jsonArray[index] = JsonSerializer.SerializeToNode(value);
    }

    public object? GetValue(string key)
    {
        var index = ValidateIndex(key);
        return jsonArray[index];
    }

    public bool TryGetValue(string key, out object? value)
    {
        var index = ParseIndex(key);
        if (!IsIndexValid(index))
        {
            value = null;
            return false;
        }
        
        value = jsonArray[index];
        return true;
    }

    public bool Remove(string key)
    {
        var index = ValidateIndex(key);
        jsonArray.RemoveAt(index);
        return true;
    }

    public bool ContainsKey(string key)
    {
        return IsIndexValid(ParseIndex(key));
    }
}