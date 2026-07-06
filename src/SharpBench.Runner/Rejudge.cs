using NetEval;

namespace SharpBench.Runner;

/// <summary>
/// Re-scores committed generations with a different idiom judge — zero new generations,
/// so any score delta is attributable to the judge alone. Each stored candidate goes back
/// through both judge layers: the Roslyn test layer is local and deterministic; only the
/// idiom layer is an API call. Output lands in a parallel root, never under results/ —
/// the leaderboard's latest-line-wins dedupe would otherwise let a rejudge silently
/// replace the published numbers.
/// </summary>
public static class Rejudge
{
    /// <param name="judgeFactory">Builds the per-task composite judge; production passes
    /// <c>task => BenchmarkRun.CreateJudge(task, idiomJudge)</c>.</param>
    /// <param name="resultsRoot">Root of the committed results to re-score.</param>
    /// <param name="rejudgeRoot">Where the re-scored JSONL files are written, one per model.
    /// Existing files are replaced, not appended — a rejudge is always a full pass.</param>
    public static async Task RunAsync(
        Func<BenchTask, IJudge> judgeFactory,
        IReadOnlyList<BenchTask> tasks,
        string resultsRoot,
        string rejudgeRoot,
        CancellationToken cancellationToken = default)
    {
        var taskByKey = tasks.ToDictionary(t => (t.Category, t.Id));
        var lines = Leaderboard.LoadLatest(resultsRoot);
        if (lines.Count == 0)
        {
            Console.Error.WriteLine($"No results found under {resultsRoot}. Run with --model <label> first.");
            return;
        }

        Directory.CreateDirectory(rejudgeRoot);
        foreach (var byModel in lines.GroupBy(l => l.Model))
        {
            var outPath = Path.Combine(rejudgeRoot, $"{FileLabel(byModel.Key)}.jsonl");
            File.Delete(outPath);

            foreach (var line in byModel.OrderBy(l => l.Category).ThenBy(l => l.Task).ThenBy(l => l.Generation))
            {
                if (!taskByKey.TryGetValue((line.Category, line.Task), out var task))
                {
                    Console.Error.WriteLine(
                        $"[{line.Model}] {line.Category}/{line.Task}: task no longer in the task set, skipped.");
                    continue;
                }

                if (line.Output is not string code)
                {
                    Console.Error.WriteLine(
                        $"[{line.Model}] {line.Category}/{line.Task} gen {line.Generation}: no stored output, skipped.");
                    continue;
                }

                // Same request shape EvalRunner builds during a live run — the judge sees
                // the original prompt as context — so scores are comparable like-for-like.
                var verdict = await judgeFactory(task).JudgeAsync(
                    new JudgeRequest(code, task.Criteria, task.Prompt), cancellationToken);

                // SUT latency and token counts describe the original generation, which is
                // not re-run; copy them through so cost/latency columns stay truthful.
                var jsonl = BenchmarkRun.SerializeResultLine(
                    line.Model, task, line.Generation, verdict,
                    line.SutLatencyMs, line.SutInputTokens, line.SutOutputTokens, code);
                await File.AppendAllTextAsync(outPath, jsonl + Environment.NewLine, cancellationToken);

                Console.WriteLine(
                    $"[{line.Model}] {line.Category}/{line.Task} gen {line.Generation}: " +
                    $"{(verdict.Passed ? "PASS" : "FAIL")} (score {verdict.Score:0.00}, was {line.Score:0.00})");
            }
        }
    }

    /// <summary>Model and judge labels can contain characters that are invalid in file
    /// names (e.g. "ollama:qwen2.5-coder:7b").</summary>
    internal static string FileLabel(string label) =>
        string.Join("-", label.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}
