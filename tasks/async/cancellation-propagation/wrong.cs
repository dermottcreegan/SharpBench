using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Classic bug: awaits each fetch in turn, so the downloads run sequentially
// instead of concurrently. Correct results, but no overlap.
public static class Downloader
{
    public static async Task<string[]> DownloadAllAsync(
        IEnumerable<Uri> uris,
        Func<Uri, CancellationToken, Task<string>> fetch,
        CancellationToken cancellationToken)
    {
        var results = new List<string>();
        foreach (var uri in uris)
            results.Add(await fetch(uri, cancellationToken));
        return results.ToArray();
    }
}
