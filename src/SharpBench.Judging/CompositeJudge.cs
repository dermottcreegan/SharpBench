using NetEval;

namespace SharpBench.Judging;

/// <summary>
/// Combines several judges into one verdict: <c>Passed</c> only when every judge
/// passes, <c>Score</c> is the weight-normalized average. SharpBench uses it to
/// pair the objective <see cref="RoslynTestJudge"/> (weight 0.7) with an idiom
/// <see cref="ChatClientJudge"/> (weight 0.3).
/// </summary>
public sealed class CompositeJudge(params (IJudge Judge, double Weight)[] judges) : IJudge
{
    public async Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        if (judges.Length == 0)
            throw new InvalidOperationException("CompositeJudge needs at least one judge.");

        var verdicts = new List<(JudgeVerdict Verdict, double Weight)>(judges.Length);
        foreach (var (judge, weight) in judges)
            verdicts.Add((await judge.JudgeAsync(request, cancellationToken), weight));

        var totalWeight = verdicts.Sum(v => v.Weight);
        var usage = verdicts.Aggregate(JudgeUsage.Zero, (sum, v) => sum + (v.Verdict.Usage ?? JudgeUsage.Zero));

        return new JudgeVerdict(
            Passed: verdicts.All(v => v.Verdict.Passed),
            Score: verdicts.Sum(v => v.Verdict.Score * v.Weight) / totalWeight,
            Reasoning: string.Join(" || ", verdicts.Select(v => v.Verdict.Reasoning)),
            Usage: usage);
    }
}
