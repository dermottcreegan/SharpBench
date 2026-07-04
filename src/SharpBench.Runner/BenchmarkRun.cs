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
    /// <summary>
    /// Overall pass bar for the weighted composite score. The functional judge is
    /// required outright; the idiom judge only moves the score, so a perfect
    /// functional result (0.7) plus a mediocre idiom score still clears the bar.
    /// </summary>
    private const double PassThreshold = 0.7;

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
                PassThreshold,
                (new RoslynTestJudge(task.HiddenTestsSource), FunctionalWeight, Required: true),
                (idiomJudge, IdiomWeight, Required: false));

            UsageDetails? sutUsage = null;
            var report = await new EvalRunner(judge).RunAsync(
                [new EvalCase(task.Prompt, task.Criteria)],
                async (input, ct) =>
                {
                    var (text, usage) = await GenerateAsync(contestant, input, ct);
                    sutUsage = usage;
                    return text;
                },
                cancellationToken: cancellationToken);

            var result = report.Results.Single();
            await AppendResultAsync(resultsPath, modelLabel, task, result, sutUsage, cancellationToken);
            Console.WriteLine(
                $"[{modelLabel}] {task.Category}/{task.Id}: " +
                $"{(result.Verdict.Passed ? "PASS" : "FAIL")} (score {result.Verdict.Score:0.00}, sut {result.SutLatency.TotalMilliseconds:0} ms)");
        }
    }

    private static async Task<(string Text, UsageDetails? Usage)> GenerateAsync(
        IChatClient contestant, string prompt, CancellationToken ct)
    {
        List<ChatMessage> messages =
        [
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, prompt),
        ];
        // Cap output uniformly across providers (Claude bakes its cap into the client;
        // OpenAI/Gemini read it from ChatOptions).
        var options = new ChatOptions { MaxOutputTokens = ChatClientFactory.MaxOutputTokens };
        var response = await contestant.GetResponseAsync(messages, options, ct);
        return (response.Text, response.Usage);
    }

    private static async Task AppendResultAsync(
        string resultsPath,
        string modelLabel,
        BenchTask task,
        EvalCaseResult result,
        UsageDetails? sutUsage,
        CancellationToken ct)
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
            // Token counts, not dollars: pricing changes; the results post can price
            // them at publication time. Null when the provider reports no usage.
            sutInputTokens = sutUsage?.InputTokenCount,
            sutOutputTokens = sutUsage?.OutputTokenCount,
            output = result.Output,
            timestampUtc = DateTime.UtcNow,
        });
        await File.AppendAllTextAsync(resultsPath, line + Environment.NewLine, ct);
    }
}
