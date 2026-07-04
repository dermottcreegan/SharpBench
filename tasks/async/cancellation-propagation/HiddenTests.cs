using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class DownloaderHiddenTests
{
    [Fact]
    public async Task Returns_results_in_input_order()
    {
        var uris = new[] { new Uri("https://x.test/1"), new Uri("https://x.test/2"), new Uri("https://x.test/3") };

        var results = await Downloader.DownloadAllAsync(
            uris,
            async (uri, ct) =>
            {
                // First URI finishes last: order must come from the input, not completion time.
                await Task.Delay(uri.AbsolutePath == "/1" ? 100 : 1, ct);
                return uri.AbsolutePath;
            },
            CancellationToken.None);

        Assert.Equal(new[] { "/1", "/2", "/3" }, results);
    }

    [Fact]
    public async Task Fetches_run_concurrently_not_sequentially()
    {
        var inFlight = 0;
        var maxInFlight = 0;

        await Downloader.DownloadAllAsync(
            Enumerable.Range(0, 4).Select(i => new Uri($"https://x.test/{i}")),
            async (uri, ct) =>
            {
                var now = Interlocked.Increment(ref inFlight);
                InterlockedMax(ref maxInFlight, now);
                await Task.Delay(100, ct);
                Interlocked.Decrement(ref inFlight);
                return "";
            },
            CancellationToken.None);

        Assert.True(maxInFlight >= 2, $"Expected concurrent fetches, but max in flight was {maxInFlight}.");
    }

    [Fact]
    public async Task Already_cancelled_token_faults_the_task()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Downloader.DownloadAllAsync(
                new[] { new Uri("https://x.test/a") },
                (uri, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return Task.FromResult("should not get here");
                },
                cts.Token));
    }

    [Fact]
    public async Task Cancellation_mid_flight_propagates_to_fetches()
    {
        using var cts = new CancellationTokenSource();

        var task = Downloader.DownloadAllAsync(
            Enumerable.Range(0, 3).Select(i => new Uri($"https://x.test/{i}")),
            async (uri, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct); // only ends early if ct was propagated
                return "";
            },
            cts.Token);

        cts.CancelAfter(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        while (value > (current = Volatile.Read(ref location)))
            if (Interlocked.CompareExchange(ref location, value, current) == current)
                return;
    }
}
