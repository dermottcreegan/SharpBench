public sealed record Money(string Currency, decimal Amount)
{
    public Money Add(decimal delta) => this with { Amount = Amount + delta };
}
