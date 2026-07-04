using Xunit;

public class LookupHiddenTests
{
    [Fact]
    public void Get_returns_stored_value()
    {
        var lookup = new Lookup();
        lookup.Add("a", "1");
        Assert.True(lookup.TryGet("a", out var value));
        Assert.Equal("1", value);
    }

    [Fact]
    public void Missing_key_returns_false_without_throwing()
    {
        // Catches reading with the indexer, which throws KeyNotFoundException.
        var lookup = new Lookup();
        Assert.False(lookup.TryGet("missing", out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Later_add_overwrites()
    {
        var lookup = new Lookup();
        lookup.Add("k", "first");
        lookup.Add("k", "second");
        lookup.TryGet("k", out var value);
        Assert.Equal("second", value);
    }
}
