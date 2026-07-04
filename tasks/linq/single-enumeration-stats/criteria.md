The code is idiomatic modern C# (.NET 8 / C# 12):

- A single `foreach` (or equivalent single-pass construct) computes both aggregates; no `.Average()` + `.Max()` double enumeration and no buffering of the sequence.
- Argument validation uses `ArgumentNullException.ThrowIfNull` or an equivalent modern pattern, and happens eagerly (not deferred inside an iterator).
- Naming follows .NET conventions; the empty-sequence case reads clearly rather than relying on sentinel values like `double.MinValue` leaking into results.
