Write a static class named `Aggregate` with exactly this public method:

```csharp
public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
```

Requirements:

- Return the sum of `values` using generic math (`System.Numerics.INumber<T>`), so the same method works for `int`, `double`, `decimal`, etc.
- An empty sequence returns `T.Zero`.
