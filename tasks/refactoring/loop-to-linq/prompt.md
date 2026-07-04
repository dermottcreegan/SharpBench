The method below builds its result with a manual loop. Rewrite the class in idiomatic modern C# using LINQ **without changing its behavior** — keep the same public class `WordStats`, the same `CountByInitial` signature, and the same result for every input.

```csharp
public static class WordStats
{
    public static Dictionary<char, int> CountByInitial(IEnumerable<string> words)
    {
        var counts = new Dictionary<char, int>();
        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
                continue;
            var key = char.ToLowerInvariant(word[0]);
            if (counts.ContainsKey(key))
                counts[key] = counts[key] + 1;
            else
                counts[key] = 1;
        }
        return counts;
    }
}
```
