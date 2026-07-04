The code is idiomatic modern C# (.NET 8 / C# 12):

- Distinguishes null from empty with `string.IsNullOrEmpty` (or an equivalent check), rather than a bare `??` chain that treats `""` as a present value.
- Reads as a short, flat sequence of checks rather than a nested `if` pyramid; compiles clean under `#nullable enable`.
- Naming follows .NET conventions.
