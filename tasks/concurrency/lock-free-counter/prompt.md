Write a `sealed class Counter` with exactly these public members:

```csharp
public void Increment();
public long Value { get; }
```

Requirements:

- `Increment` raises the count by one; `Value` returns the current count. The count starts at `0`.
- `Increment` must be safe to call concurrently from many threads with no lost updates.
