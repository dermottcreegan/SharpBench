// Classic bug: a mutable class instead of a record — it loses value equality, and
// Add mutates in place and hands back the same instance.
public sealed class Money
{
    public string Currency { get; }
    public decimal Amount { get; private set; }

    public Money(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }

    public Money Add(decimal delta)
    {
        Amount += delta;
        return this;
    }
}
