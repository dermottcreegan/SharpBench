using System.Collections.Generic;
using System.Linq;

// Classic bug: the LINQ rewrite drops the empty-string guard and the lower-casing,
// so it throws on empty/null words and treats "Apple" and "apple" as different keys.
public static class WordStats
{
    public static Dictionary<char, int> CountByInitial(IEnumerable<string> words)
    {
        return words
            .GroupBy(word => word[0])
            .ToDictionary(group => group.Key, group => group.Count());
    }
}
