Write a static class named `Stats` with exactly this public method:

```csharp
public static (double Mean, double Max) MeanAndMax(IEnumerable<double> values)
```

Requirements:

- Returns the arithmetic mean and the maximum of the sequence.
- Enumerates the source sequence **at most once** (assume it may be a non-replayable stream, e.g. reading from the network).
- Throws `ArgumentException` when the sequence is empty, and `ArgumentNullException` when it is null.
- Does not materialize the whole sequence into a collection (no `ToList`/`ToArray`); memory use must be O(1).
