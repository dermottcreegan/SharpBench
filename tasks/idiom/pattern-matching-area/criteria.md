The code is idiomatic modern C# (.NET 8 / C# 12):

- Dispatches on shape type with a `switch` expression and type patterns, not a chain of `if (x is T)` / `as` casts or a type-code enum.
- The switch is exhaustive with a discard arm (`_`) that throws for unknown shapes, rather than silently returning a default.
- Reads cleanly; naming follows .NET conventions.
