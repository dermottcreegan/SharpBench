The code is idiomatic modern C# (.NET 8 / C# 12) and behavior-preserving:

- Replaces the `if`/`return` ladder with a `switch` expression or pattern matching, and uses `??` / string interpolation instead of `+` concatenation and `.ToString()`.
- Behavior is unchanged for every input — in particular a `null` count ("unknown") stays distinct from a `0` count ("no").
- Naming follows .NET conventions; no dead code.
