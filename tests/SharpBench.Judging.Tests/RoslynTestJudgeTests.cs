using NetEval;
using SharpBench.Judging;
using Xunit;

namespace SharpBench.Judging.Tests;

/// <summary>
/// Judge-mechanics regressions that go beyond the generic task contract
/// (<see cref="TaskContractTests"/>): markdown unwrapping, compilation-failure
/// reporting, and partial scoring with the specific failing fact named. Candidate
/// solutions are loaded from the real task folders' reference.cs / wrong.cs.
/// </summary>
public class RoslynTestJudgeTests
{
    private static Task<JudgeVerdict> JudgeAsync(string category, string id, string candidateCode) =>
        new RoslynTestJudge(TaskFiles.Read(category, id, "HiddenTests.cs"))
            .JudgeAsync(new JudgeRequest(candidateCode, Criteria: "unused by this judge"));

    [Fact]
    public async Task Fenced_markdown_response_is_unwrapped_before_compiling()
    {
        var reference = TaskFiles.Read("linq", "single-enumeration-stats", "reference.cs");
        var fenced = $"Here is the implementation:\n\n```csharp\n{reference}\n```\n";

        var verdict = await JudgeAsync("linq", "single-enumeration-stats", fenced);
        Assert.True(verdict.Passed, verdict.Reasoning);
    }

    [Fact]
    public async Task Non_compiling_response_fails_with_diagnostics()
    {
        var verdict = await JudgeAsync("linq", "single-enumeration-stats", "This is prose, not a C# file.");
        Assert.False(verdict.Passed);
        Assert.Equal(0.0, verdict.Score);
        Assert.StartsWith("Compilation failed", verdict.Reasoning);
    }

    [Fact]
    public async Task Double_enumeration_solution_names_the_failing_fact()
    {
        var wrong = TaskFiles.Read("linq", "single-enumeration-stats", "wrong.cs");

        var verdict = await JudgeAsync("linq", "single-enumeration-stats", wrong);
        Assert.False(verdict.Passed);
        Assert.Contains("Enumerates_the_source_exactly_once", verdict.Reasoning);
    }

    [Fact]
    public async Task Sequential_downloader_fails_only_the_concurrency_fact_and_scores_partially()
    {
        var wrong = TaskFiles.Read("async", "cancellation-propagation", "wrong.cs");

        var verdict = await JudgeAsync("async", "cancellation-propagation", wrong);
        Assert.False(verdict.Passed);
        Assert.Contains("Fetches_run_concurrently_not_sequentially", verdict.Reasoning);
        Assert.Equal(0.75, verdict.Score); // 3 of 4 hidden tests still pass
    }
}
