using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using HsMan.NestedStore;

namespace CliRelay.NestedStores;

public class JsonObjectNestedStoreBackend(JsonObject jsonObject) : INestedStoreBackend
{
    public void SetValue(string key, object? value)
    {
        var jsonNode = JsonSerializer.SerializeToNode(value);
        jsonObject[key] = jsonNode;
    }

    public object? GetValue(string key) => jsonObject[key];
    
    public bool TryGetValue(string key, out object? value)
    {
        if (!jsonObject.TryGetPropertyValue(key, out var val))
        {
            value = null;
            return false;
        }
        
        value = val;
        return true;
    }

    public bool Remove(string key) => jsonObject.Remove(key);
    public bool ContainsKey(string key) => jsonObject.ContainsKey(key);
   
}