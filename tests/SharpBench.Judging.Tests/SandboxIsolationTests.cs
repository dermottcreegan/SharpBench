using System.Diagnostics;
using NetEval;
using SharpBench.Judging;
using Xunit;

namespace SharpBench.Judging.Tests;

/// <summary>
/// Proves the sandbox actually isolates the harness: a candidate that would hang
/// the run is contained by RoslynTestJudge's hard wall-clock timeout, which kills
/// the child process tree instead of wedging the parent.
/// </summary>
public class SandboxIsolationTests
{
    // A candidate whose method blocks far longer than any timeout below.
    private const string HangingCandidate =
        """
        using System.Threading;
        public static class Slow
        {
            public static void Run() => Thread.Sleep(60_000);
        }
        """;

    private const string HangingTest =
        """
        using Xunit;
        public class HangTests
        {
            [Fact]
            public void Calls_the_slow_method() => Slow.Run();
        }
        """;

    [Fact]
    public async Task Hanging_candidate_is_killed_at_the_hard_timeout_not_left_to_wedge_the_run()
    {
        var judge = new RoslynTestJudge(HangingTest, hardTimeout: TimeSpan.FromSeconds(2));

        var stopwatch = Stopwatch.StartNew();
        var verdict = await judge.JudgeAsync(new JudgeRequest(HangingCandidate, Criteria: "unused"));
        stopwatch.Stop();

        Assert.False(verdict.Passed);
        Assert.Equal(0.0, verdict.Score);
        Assert.Contains("limit", verdict.Reasoning);
        // The parent kill must land near the 2s limit, well before the 60s hang.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30),
            $"Timeout enforcement took {stopwatch.Elapsed.TotalSeconds:0}s — the parent did not kill the sandbox promptly.");
    }
}
