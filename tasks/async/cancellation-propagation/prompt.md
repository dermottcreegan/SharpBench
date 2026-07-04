Write a static class named `Downloader` with exactly this public method:

```csharp
public static async Task<string[]> DownloadAllAsync(
    IEnumerable<Uri> uris,
    Func<Uri, CancellationToken, Task<string>> fetch,
    CancellationToken cancellationToken)
```

Requirements:

- Fetch all URIs concurrently using the provided `fetch` delegate.
- Return the results in the same order as the input `uris`.
- Propagate `cancellationToken` to every `fetch` call, and stop promptly when it is cancelled (the returned task must fault with an `OperationCanceledException`).
- No blocking calls (`.Result`, `.Wait()`, `Thread.Sleep`).
