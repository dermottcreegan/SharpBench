The code is idiomatic modern C# (.NET 8 / C# 12) and behavior-preserving:

- Expresses the grouping with `GroupBy` + `ToDictionary` rather than manual `ContainsKey`/indexer bookkeeping.
- Preserves the original behavior exactly: skips null/empty words (`Where`) and folds case with `char.ToLowerInvariant` before grouping.
- Reads as a single clear query; naming follows .NET conventions.
