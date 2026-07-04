using System;
using Xunit;

public class AggregateHiddenTests
{
    [Fact]
    public void Sums_integers()
    {
        Assert.Equal(6, Aggregate.Sum(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void Sums_doubles()
    {
        Assert.Equal(7.5, Aggregate.Sum(new[] { 2.5, 5.0 }));
    }

    [Fact]
    public void Empty_sequence_is_zero()
    {
        // Catches reducing from the first element instead of seeding with T.Zero,
        // which throws on an empty sequence.
        Assert.Equal(0, Aggregate.Sum(Array.Empty<int>()));
    }
}
