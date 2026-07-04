Write a static class named `SpanParser` with exactly this public method:

```csharp
public static int SumCsvInts(ReadOnlySpan<char> csv)
```

Requirements:

- Parse the comma-separated base-10 integers in `csv` and return their sum.
- Values may be negative. An empty span sums to `0`.
- Do not allocate: no `String.Split`, no `ToString`, no substrings — parse directly over the span (e.g. `int.Parse(ReadOnlySpan<char>)`).
