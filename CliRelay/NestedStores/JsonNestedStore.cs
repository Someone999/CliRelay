using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using HsMan.NestedStore;

namespace CliRelay.NestedStores;

public class JsonNestedStore(JsonPath path, INestedStoreBackend backend) : INestedStore
{
    private readonly ConcurrentDictionary<string, INestedStoreBackend> _backendsCache = new();
    private readonly ConcurrentDictionary<string, JsonNestedStore>  _nestedStoreCache = new();

    public JsonPath Path { get; } = path;

    public object? GetValue(string key) => backend.GetValue(key);

    private INestedStoreBackend? GetCachedBackend<T>(string key, JsonNode node) where T : class, INestedStoreBackend
    {
       var val = _backendsCache.GetValueOrDefault(key);
       if (val is not null)
       {
           return val as T;
       }

       INestedStoreBackend? cachedBackend = null;
       switch (node)
       {
           case JsonObject jsonObject:
               cachedBackend = new JsonObjectNestedStoreBackend(jsonObject);
               _backendsCache.TryAdd(key, cachedBackend);
               break;
           case JsonArray jsonArray:
               cachedBackend = new JsonArrayNestedStoreBackend(jsonArray);
               _backendsCache.TryAdd(key, cachedBackend);
               break;
       }

       return cachedBackend;
    }
    
    public INestedStore GetNestedStore(string key)
    {
        if (!backend.TryGetValue(key, out var value))
        {
            throw new KeyNotFoundException($"Key '{key}' not found");
        }

        if (_nestedStoreCache.TryGetValue(key, out var nestedStore))
        {
            return nestedStore;
        }

        var nextPath = Path.Append(key);
        INestedStoreBackend? backend1 = null;
        switch (value)
        {
            case JsonObject jsonObject:
                backend1 = GetCachedBackend<JsonObjectNestedStoreBackend>(key, jsonObject);
                break;
            case JsonArray jsonArray:
                backend1 = GetCachedBackend<JsonArrayNestedStoreBackend>(key, jsonArray);
                break;
        }

        if (backend1 == null)
        {
            var err = $"Failed to adapt object type: {value?.GetType().ToString() ?? "null"} to json types";
            throw new InvalidDataException(err);
        }
        
        var nestedStoreCache = new JsonNestedStore(nextPath, backend1);
        _nestedStoreCache.TryAdd(key, nestedStoreCache);
        return nestedStoreCache;
    }

    public bool IsNull(string key)
    {
        var val = GetValue(key);
        return val == null;
    }

    public bool ContainsKey(string key) => backend.ContainsKey(key);
   

    public bool TryGetValue<T>(string key, out T? value)
    {
        var orig = GetValue(key);
        if (!ContainsKey(key))
        {
            value = default;
            return false;
        }

        if (orig == null)
        {
            value = default;
            return true;
        }
        
        if (orig is JsonNode node)
        {
            if (typeof(T) == typeof(JsonObject) || typeof(T) == typeof(JsonArray) || typeof(T) == typeof(JsonValue))
            {
                value = (T)orig;
                return true;
            }
            
            try
            {
                var val = node.GetValue<T>();
                value = val;
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }

        try
        {
            value = (T)orig;
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }
}