The code is idiomatic modern C# (.NET 8 / C# 12):

- Uses a positional `record` for value equality rather than a hand-written class with manual `Equals`/`GetHashCode`.
- `Add` produces the updated copy with a `with` expression, not by constructing a new instance field-by-field and not by mutating state.
- The type is immutable (no settable properties); naming follows .NET conventions.
