using NetEval;
using SharpBench.Judging;
using Xunit;

namespace SharpBench.Judging.Tests;

/// <summary>
/// Pass/fail semantics of the composite verdict: required judges gate outright,
/// advisory judges only move the weighted score against the pass threshold. Mirrors
/// the runner's real configuration (functional 0.7 required, idiom 0.3 advisory,
/// threshold 0.7).
/// </summary>
public class CompositeJudgeTests
{
    private sealed class StubJudge(bool passed, double score) : IJudge
    {
        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new JudgeVerdict(passed, score, $"stub({passed}, {score})"));
    }

    private static Task<JudgeVerdict> JudgeAsync(
        (bool Passed, double Score) functional, (bool Passed, double Score) idiom) =>
        new CompositeJudge(
                0.7,
                (new StubJudge(functional.Passed, functional.Score), 0.7, Required: true),
                (new StubJudge(idiom.Passed, idiom.Score), 0.3, Required: false))
            .JudgeAsync(new JudgeRequest("output", "criteria"));

    [Fact]
    public async Task Advisory_nitpick_cannot_veto_a_functionally_perfect_solution()
    {
        // The dry-run failure mode: all hidden tests pass, idiom judge says "not passed".
        var verdict = await JudgeAsync(functional: (true, 1.0), idiom: (false, 0.8));

        Assert.True(verdict.Passed, verdict.Reasoning);
        Assert.Equal(0.94, verdict.Score, precision: 2);
    }

    [Fact]
    public async Task Functional_pass_alone_clears_the_threshold_even_with_idiom_score_zero()
    {
        var verdict = await JudgeAsync(functional: (true, 1.0), idiom: (false, 0.0));

        Assert.True(verdict.Passed);
        Assert.Equal(0.7, verdict.Score, precision: 2);
    }

    [Fact]
    public async Task Required_judge_failure_vetoes_regardless_of_score()
    {
        // Partial hidden-test pass can weight-average above the threshold; still a fail.
        var verdict = await JudgeAsync(functional: (false, 0.9), idiom: (true, 1.0));

        Assert.False(verdict.Passed);
    }

    [Fact]
    public async Task Weighted_score_below_threshold_fails_even_when_required_judge_passes()
    {
        var judge = new CompositeJudge(
            0.7,
            (new StubJudge(passed: true, score: 0.6), 0.7, Required: true),
            (new StubJudge(passed: true, score: 0.5), 0.3, Required: false));

        var verdict = await judge.JudgeAsync(new JudgeRequest("output", "criteria"));

        Assert.False(verdict.Passed);
        Assert.Equal(0.57, verdict.Score, precision: 2);
    }
}
