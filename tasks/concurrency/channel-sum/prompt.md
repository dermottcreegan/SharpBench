Write a static class named `Pipeline` with exactly this public method:

```csharp
public static Task<long> ProduceConsumeSumAsync(int count)
```

Requirements:

- Produce the integers `0, 1, …, count-1` into a **bounded** `Channel<int>` from a producer task, and consume them concurrently, returning their sum as a `long`.
- Use a bounded channel so the producer's writes await when it is full (back-pressure).
- Complete the channel's writer when production finishes so the consumer terminates.
- `count == 0` returns `0`.
