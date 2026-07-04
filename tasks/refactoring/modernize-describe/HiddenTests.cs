using Xunit;

public class InventoryHiddenTests
{
    [Fact]
    public void Null_count_is_unknown()
    {
        Assert.Equal("unknown items", Inventory.Describe(null, null));
    }

    [Fact]
    public void Zero_count_is_none()
    {
        // Catches conflating a null count with a zero count during the rewrite.
        Assert.Equal("no items", Inventory.Describe(0, null));
    }

    [Fact]
    public void Positive_count_with_label()
    {
        Assert.Equal("3 apples", Inventory.Describe(3, "apples"));
    }

    [Fact]
    public void Null_count_with_label()
    {
        Assert.Equal("unknown box", Inventory.Describe(null, "box"));
    }
}
