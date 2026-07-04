The code is idiomatic modern C# (.NET 8 / C# 12):

- Uses the `INumber<T>` generic-math constraint and seeds the accumulator with `T.Zero`, rather than boxing to `object`/`dynamic` or writing per-type overloads.
- A single `foreach` accumulates the total; the empty case falls out naturally from the `T.Zero` seed rather than a special-case guard.
- Naming follows .NET conventions.
