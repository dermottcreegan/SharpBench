The code is idiomatic modern C# (.NET 8 / C# 12):

- Parses directly over the span using `int.Parse(ReadOnlySpan<char>)` on slices, with no heap allocation — no `Split`, `ToString`, `Substring`, or intermediate collections.
- Slicing is done with range indexing / `IndexOf` on the span rather than copying.
- Handles the final segment and negative values without special-case clutter; naming follows .NET conventions.
