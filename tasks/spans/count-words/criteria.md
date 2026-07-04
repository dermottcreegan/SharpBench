The code is idiomatic modern C# (.NET 8 / C# 12):

- Makes a single pass over the span tracking word boundaries, with no heap allocation — no `Split`, `ToString`, `Trim`, or intermediate arrays.
- Uses `char.IsWhiteSpace` rather than comparing against a single `' '` literal, so tabs and newlines are handled.
- The boundary logic is expressed clearly; naming follows .NET conventions.
