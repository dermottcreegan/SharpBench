The code is idiomatic modern C# (.NET 8 / C# 12):

- Uses async/await end to end; no sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`), no `async void`.
- The cancellation token is passed through to every awaited operation rather than checked only once at the top.
- Concurrency is expressed with `Task.WhenAll` (or equivalent), not manual thread management or fire-and-forget.
- Nullable reference types would compile clean; naming follows .NET conventions; no dead code or unnecessary allocations (e.g. no `ToList()` just to iterate).
