Write a `sealed record Money` with exactly this shape and method:

```csharp
public sealed record Money(string Currency, decimal Amount)
{
    public Money Add(decimal delta);
}
```

Requirements:

- `Money` has value equality: two `Money` values with the same `Currency` and `Amount` are equal.
- `Add` returns a `Money` with the same `Currency` and `Amount` increased by `delta`.
- `Add` must not mutate the instance it is called on.
