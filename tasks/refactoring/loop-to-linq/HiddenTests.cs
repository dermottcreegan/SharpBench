using System.Collections.Generic;
using Xunit;

public class WordStatsHiddenTests
{
    [Fact]
    public void Groups_and_counts_by_first_letter()
    {
        var result = WordStats.CountByInitial(new[] { "apple", "avocado", "banana" });
        Assert.Equal(2, result['a']);
        Assert.Equal(1, result['b']);
    }

    [Fact]
    public void Is_case_insensitive()
    {
        // Catches dropping the lower-casing during the rewrite.
        var result = WordStats.CountByInitial(new[] { "Apple", "apple" });
        Assert.Equal(2, result['a']);
    }

    [Fact]
    public void Skips_null_and_empty_words()
    {
        // Catches removing the guard, which then throws on word[0].
        var result = WordStats.CountByInitial(new string[] { "a", "", null, "b" });
        Assert.Equal(1, result['a']);
        Assert.Equal(1, result['b']);
    }

    [Fact]
    public void Empty_input_is_empty()
    {
        Assert.Empty(WordStats.CountByInitial(new string[0]));
    }
}
