using NetEval;

namespace SharpBench.Judging;

/// <summary>
/// Combines several judges into one verdict: <c>Score</c> is the weight-normalized
/// average, and <c>Passed</c> requires every judge marked <c>Required</c> to pass
/// plus the weighted score to reach <paramref name="passThreshold"/>. SharpBench
/// pairs the objective <see cref="RoslynTestJudge"/> (weight 0.7, required) with an
/// idiom <see cref="ChatClientJudge"/> (weight 0.3, advisory) — a style nitpick
/// lowers the score but cannot veto a functionally correct solution.
/// </summary>
public sealed class CompositeJudge(
    double passThreshold,
    params (IJudge Judge, double Weight, bool Required)[] judges) : IJudge
{
    public async Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        if (judges.Length == 0)
            throw new InvalidOperationException("CompositeJudge needs at least one judge.");

        var verdicts = new List<(JudgeVerdict Verdict, double Weight, bool Required)>(judges.Length);
        foreach (var (judge, weight, required) in judges)
            verdicts.Add((await judge.JudgeAsync(request, cancellationToken), weight, required));

        var totalWeight = verdicts.Sum(v => v.Weight);
        var usage = verdicts.Aggregate(JudgeUsage.Zero, (sum, v) => sum + (v.Verdict.Usage ?? JudgeUsage.Zero));
        var score = verdicts.Sum(v => v.Verdict.Score * v.Weight) / totalWeight;

        return new JudgeVerdict(
            Passed: verdicts.Where(v => v.Required).All(v => v.Verdict.Passed) && score >= passThreshold,
            Score: score,
            Reasoning: string.Join(" || ", verdicts.Select(v => v.Verdict.Reasoning)),
            Usage: usage);
    }
}
