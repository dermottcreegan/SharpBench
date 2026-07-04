using Xunit;

public class ConfigHiddenTests
{
    [Fact]
    public void Uses_primary_when_present()
    {
        Assert.Equal("p", Config.Resolve("p", "fb", "def"));
    }

    [Fact]
    public void Skips_null_primary()
    {
        Assert.Equal("fb", Config.Resolve(null, "fb", "def"));
    }

    [Fact]
    public void Skips_empty_primary()
    {
        // Catches a bare ?? chain that treats "" as a present value.
        Assert.Equal("fb", Config.Resolve("", "fb", "def"));
    }

    [Fact]
    public void Falls_back_to_default()
    {
        Assert.Equal("def", Config.Resolve(null, null, "def"));
    }

    [Fact]
    public void Empty_values_fall_back_to_default()
    {
        Assert.Equal("def", Config.Resolve("", "", "def"));
    }
}
