Write a static class named `TextScan` with exactly this public method:

```csharp
public static int CountWords(ReadOnlySpan<char> text)
```

Requirements:

- Return the number of whitespace-separated words in `text`.
- A run of whitespace is a single separator; leading and trailing whitespace do not create empty words.
- Whitespace-only or empty input has `0` words.
- Do not allocate: no `String.Split`, no `ToString`, no regex — scan the span directly (e.g. `char.IsWhiteSpace`).
