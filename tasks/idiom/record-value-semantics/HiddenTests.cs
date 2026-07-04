using Xunit;

public class MoneyHiddenTests
{
    [Fact]
    public void Values_with_same_fields_are_equal()
    {
        Assert.Equal(new Money("USD", 5m), new Money("USD", 5m));
    }

    [Fact]
    public void Add_returns_updated_amount_and_same_currency()
    {
        var updated = new Money("USD", 5m).Add(3m);
        Assert.Equal("USD", updated.Currency);
        Assert.Equal(8m, updated.Amount);
    }

    [Fact]
    public void Add_does_not_mutate_the_original()
    {
        // Catches an in-place mutation that changes the caller's value.
        var original = new Money("USD", 5m);
        original.Add(3m);
        Assert.Equal(5m, original.Amount);
    }

    [Fact]
    public void Add_returns_a_new_instance()
    {
        var original = new Money("USD", 5m);
        Assert.NotSame(original, original.Add(1m));
    }
}
