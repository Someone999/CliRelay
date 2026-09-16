namespace CliRelay.Utils;

public static class DictionaryUtils
{
    public static void Merge(IDictionary<string, string?> left, IDictionary<string, string?> right, bool overwrite = true)
    {
        if (right.Count == 0)
        {
            return;
        }

        foreach (var kvp in right)
        {
            if (!left.TryAdd(kvp.Key, kvp.Value) && overwrite)
            {
                left[kvp.Key] = kvp.Value;
            }
        }
    }
}