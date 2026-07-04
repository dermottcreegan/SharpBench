#nullable enable
using System.Collections.Generic;

// Classic bug: reads with the indexer, which throws KeyNotFoundException for an
// absent key instead of returning false with a null value.
public sealed class Lookup
{
    private readonly Dictionary<string, string> _entries = new();

    public void Add(string key, string value) => _entries[key] = value;

    public bool TryGet(string key, out string? value)
    {
        value = _entries[key];
        return true;
    }
}
