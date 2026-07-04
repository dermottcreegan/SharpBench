#nullable enable
using System.Collections.Generic;

public sealed class Lookup
{
    private readonly Dictionary<string, string> _entries = new();

    public void Add(string key, string value) => _entries[key] = value;

    public bool TryGet(string key, out string? value)
    {
        if (_entries.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }
        value = null;
        return false;
    }
}
