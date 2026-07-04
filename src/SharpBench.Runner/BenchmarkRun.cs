using System.Text.Json;
using Microsoft.Extensions.AI;
using NetEval;
using SharpBench.Judging;

namespace SharpBench.Runner;

/// <summary>Runs every task against one model and appends per-task results to a JSONL file.</summary>
public static class BenchmarkRun
{
    /// <summary>Shared contract every contestant gets. Published verbatim in the results post.</summary>
    private const string SystemPrompt =
        """
        You are an expert C# developer. Answer with a single, complete, compilable C# file.
        Include every using directive you need. Do not use markdown code fences.
        Do not include a Main method or top-level statements unless the task asks for one.
        Target .NET 8 / C# 12.
        """;

    private const double FunctionalWeight = 0.7;
    private const double IdiomWeight = 0.3;

    /// <param name="modelLabel">Human-readable model name for the results file (e.g. "claude-sonnet-5").</param>
    /// <param name="contestant">Chat client for the model under test.</param>
    /// <param name="idiomJudge">LLM judge for the style layer — one fixed judge model for all contestants.</param>
    /// <param name="resultsPath">JSONL file to append one result line per task to.</param>
    public static async Task RunModelAsync(
        string modelLabel,
        IChatClient contestant,
        IJudge idiomJudge,
        IReadOnlyList<BenchTask> tasks,
        string resultsPath,
        CancellationToken cancellationToken = default)
    {
        foreach (var task in tasks)
        {
            // TODO(runs): wrap in 3 generations per task and report mean pass rate,
            // mirroring NetEval's Runs/PassThreshold semantics.
            var judge = new CompositeJudge(
                (new RoslynTestJudge(task.HiddenTestsSource), FunctionalWeight),
                (idiomJudge, IdiomWeight));

            var report = await new EvalRunner(judge).RunAsync(
                [new EvalCase(task.Prompt, task.Criteria)],
                (input, ct) => GenerateAsync(contestant, input, ct),
                cancellationToken: cancellationToken);

            var result = report.Results.Single();
            await AppendResultAsync(resultsPath, modelLabel, task, result, cancellationToken);
            Console.WriteLine(
                $"[{modelLabel}] {task.Category}/{task.Id}: " +
                $"{(result.Verdict.Passed ? "PASS" : "FAIL")} (score {result.Verdict.Score:0.00}, sut {result.SutLatency.TotalMilliseconds:0} ms)");
        }
    }

    private static async Task<string> GenerateAsync(IChatClient contestant, string prompt, CancellationToken ct)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, prompt),
        ];
        // Cap output uniformly across providers (Claude bakes its cap into the client;
        // OpenAI/Gemini read it from ChatOptions).
        var options = new ChatOptions { MaxOutputTokens = ChatClientFactory.MaxOutputTokens };
        // TODO(cost): capture response.Usage here so the results post can report
        // cost-per-task for the contestant, not just the judge.
        var response = await contestant.GetResponseAsync(messages, options, ct);
        return response.Text;
    }

    private static async Task AppendResultAsync(
        string resultsPath, string modelLabel, BenchTask task, EvalCaseResult result, CancellationToken ct)
    {
        var line = JsonSerializer.Serialize(new
        {
            model = modelLabel,
            category = task.Category,
            task = task.Id,
            passed = result.Verdict.Passed,
            score = result.Verdict.Score,
            reasoning = result.Verdict.Reasoning,
            sutLatencyMs = (int)result.SutLatency.TotalMilliseconds,
            output = result.Output,
            timestampUtc = DateTime.UtcNow,
        });
        await File.AppendAllTextAsync(resultsPath, line + Environment.NewLine, ct);
    }
}
