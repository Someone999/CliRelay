namespace CliRelay.NestedStores;

public class JsonPath
{ 
    private JsonPath(string path)
    {
        Path = path;
    }

    public string Path { get; }
    public JsonPath Append(string path)
    {
        if (path.Contains('.'))
        {
            throw new ArgumentException($"Path '{path}' cannot contain a dot");
        }
        
        var nextPath = Path + "." + path;
        return new JsonPath(nextPath);
    }

    public static JsonPath CreateRoot()
    {
        return new JsonPath("$");
    }
}