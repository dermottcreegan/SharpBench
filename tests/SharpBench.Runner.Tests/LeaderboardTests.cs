using SharpBench.Runner;
using Xunit;

namespace SharpBench.Runner.Tests;

/// <summary>
/// Exercises <see cref="Leaderboard.Render"/> against synthetic results directories:
/// the majority-of-generations pass rule, latest-line-wins dedupe, mixed-generation
/// flagging, pre-generation-field backward compatibility, and pricing lookups.
/// </summary>
public sealed class LeaderboardTests : IDisposable
{
    private static readonly IReadOnlyList<BenchTask> TwoTasks =
    [
        new("task-a", "async", "prompt", "criteria", "tests"),
        new("task-b", "linq", "prompt", "criteria", "tests"),
    ];

    private readonly string _root = Directory.CreateTempSubdirectory("sharpbench-lb-").FullName;

    private string ResultsDir => Path.Combine(_root, "results");
    private string PricingPath => Path.Combine(_root, "pricing.json");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteResults(string fileName, params string[] jsonLines)
    {
        Directory.CreateDirectory(ResultsDir);
        File.WriteAllLines(Path.Combine(ResultsDir, fileName), jsonLines);
    }

    private string Render() => Leaderboard.Render(ResultsDir, TwoTasks, PricingPath);

    private static string Line(
        string model, string task, int generation, bool passed, double score,
        string timestamp = "2026-07-05T10:00:00Z", long? inputTokens = 100, long? outputTokens = 200)
    {
        var category = task == "task-a" ? "async" : "linq";
        var tokens = inputTokens is null || outputTokens is null
            ? ""
            : $"\"sutInputTokens\":{inputTokens},\"sutOutputTokens\":{outputTokens},";
        return $"{{\"model\":\"{model}\",\"category\":\"{category}\",\"task\":\"{task}\"," +
               $"\"generation\":{generation},\"passed\":{(passed ? "true" : "false")},\"score\":{score:0.0#}," +
               $"\"sutLatencyMs\":1000,{tokens}\"timestampUtc\":\"{timestamp}\"}}";
    }

    [Fact]
    public void Task_passes_when_at_least_half_its_generations_pass()
    {
        WriteResults("m.jsonl",
            Line("model-x", "task-a", 1, passed: true, 0.9),
            Line("model-x", "task-a", 2, passed: true, 0.9),
            Line("model-x", "task-a", 3, passed: false, 0.3),
            Line("model-x", "task-b", 1, passed: true, 0.9),
            Line("model-x", "task-b", 2, passed: false, 0.3),
            Line("model-x", "task-b", 3, passed: false, 0.3));

        var report = Render();

        // task-a: 2/3 generations pass -> passed; task-b: 1/3 -> failed.
        Assert.Contains("| model-x | 1/2 | 3 |", report);
        Assert.Contains("- model-x: linq/task-b (1/3 generations passed, mean score 0.50)", report);
        Assert.DoesNotContain("task-a (", report);
    }

    [Fact]
    public void Latest_line_wins_per_generation_so_reruns_replace_rather_than_dupe()
    {
        WriteResults("m.jsonl",
            Line("model-x", "task-a", 1, passed: false, 0.2, timestamp: "2026-07-01T10:00:00Z"),
            Line("model-x", "task-a", 1, passed: true, 1.0, timestamp: "2026-07-05T10:00:00Z"),
            Line("model-x", "task-b", 1, passed: true, 1.0));

        var report = Render();

        Assert.Contains("| model-x | 2/2 | 1 |", report);
        Assert.DoesNotContain("## Failed tasks", report);
    }

    [Fact]
    public void Mixed_generation_counts_render_as_a_range()
    {
        WriteResults("m.jsonl",
            Line("model-x", "task-a", 1, passed: true, 1.0),
            Line("model-x", "task-a", 2, passed: true, 1.0),
            Line("model-x", "task-a", 3, passed: true, 1.0),
            Line("model-x", "task-b", 1, passed: true, 1.0));

        Assert.Contains("| model-x | 2/2 | 1–3 |", Render());
    }

    [Fact]
    public void Lines_without_a_generation_field_load_as_generation_one()
    {
        // The shape written before multi-generation runs existed.
        WriteResults("m.jsonl",
            "{\"model\":\"model-x\",\"category\":\"async\",\"task\":\"task-a\",\"passed\":true,\"score\":1.0," +
            "\"sutLatencyMs\":1000,\"sutInputTokens\":100,\"sutOutputTokens\":200," +
            "\"timestampUtc\":\"2026-07-05T10:00:00Z\"}");

        Assert.Contains("| model-x | 1/2 of 1 run | 1 |", Render());
    }

    [Fact]
    public void Cost_column_prices_from_pricing_json_and_covers_every_generation()
    {
        File.WriteAllText(PricingPath,
            "{\"asOf\":\"2026-07-05\",\"unit\":\"USD per million tokens\"," +
            "\"models\":{\"model-x\":{\"input\":10.00,\"output\":20.00}}}");
        WriteResults("m.jsonl",
            Line("model-x", "task-a", 1, passed: true, 1.0),
            Line("model-x", "task-a", 2, passed: true, 1.0));

        // 200 in * $10/MTok + 400 out * $20/MTok = $0.01.
        Assert.Contains("| $0.010 |", Render());
        Assert.Contains("as of 2026-07-05", Render());
    }

    [Fact]
    public void Cost_column_degrades_when_pricing_or_usage_is_missing()
    {
        WriteResults("m.jsonl",
            Line("unpriced-model", "task-a", 1, passed: true, 1.0),
            Line("ollama:local-model", "task-a", 1, passed: true, 1.0, inputTokens: null, outputTokens: null));

        var report = Render(); // No pricing.json written at all.

        Assert.Contains("| unpriced-model | ", report);
        Assert.Contains("| — |", report);
        Assert.Contains("| $0 (local) |", report);
        Assert.DoesNotContain("pricing.json` (as of", report);
    }
}
