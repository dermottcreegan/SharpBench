using System;
using Xunit;

public class TextScanHiddenTests
{
    [Fact]
    public void Counts_simple_words()
    {
        Assert.Equal(3, TextScan.CountWords("one two three"));
    }

    [Fact]
    public void Collapses_runs_of_whitespace()
    {
        // Catches counting each gap between words as a separate word.
        Assert.Equal(2, TextScan.CountWords("a    b"));
    }

    [Fact]
    public void Ignores_leading_and_trailing_whitespace()
    {
        Assert.Equal(2, TextScan.CountWords("   a b   "));
    }

    [Fact]
    public void Whitespace_only_is_zero()
    {
        Assert.Equal(0, TextScan.CountWords("    "));
    }

    [Fact]
    public void Empty_span_is_zero()
    {
        Assert.Equal(0, TextScan.CountWords(ReadOnlySpan<char>.Empty));
    }
}
