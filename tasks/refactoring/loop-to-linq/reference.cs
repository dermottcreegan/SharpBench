using System.Collections.Generic;
using System.Linq;

public static class WordStats
{
    public static Dictionary<char, int> CountByInitial(IEnumerable<string> words)
    {
        return words
            .Where(word => !string.IsNullOrEmpty(word))
            .GroupBy(word => char.ToLowerInvariant(word[0]))
            .ToDictionary(group => group.Key, group => group.Count());
    }
}
