using System;
using Xunit;

public class SpanParserHiddenTests
{
    [Fact]
    public void Sums_multiple_values()
    {
        Assert.Equal(6, SpanParser.SumCsvInts("1,2,3"));
    }

    [Fact]
    public void Single_value_without_comma()
    {
        // Catches dropping a lone value that has no trailing comma.
        Assert.Equal(42, SpanParser.SumCsvInts("42"));
    }

    [Fact]
    public void Includes_the_trailing_segment()
    {
        // Catches forgetting the final segment after the last comma.
        Assert.Equal(60, SpanParser.SumCsvInts("10,20,30"));
    }

    [Fact]
    public void Handles_negatives()
    {
        Assert.Equal(0, SpanParser.SumCsvInts("-5,5"));
    }

    [Fact]
    public void Empty_span_is_zero()
    {
        Assert.Equal(0, SpanParser.SumCsvInts(ReadOnlySpan<char>.Empty));
    }
}
