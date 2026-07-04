using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Downloader
{
    public static async Task<string[]> DownloadAllAsync(
        IEnumerable<Uri> uris,
        Func<Uri, CancellationToken, Task<string>> fetch,
        CancellationToken cancellationToken)
    {
        var downloads = uris.Select(uri => fetch(uri, cancellationToken)).ToArray();
        return await Task.WhenAll(downloads);
    }
}
