using System.Text.Json;
using NetEval;
using Xunit;

namespace SharpBench.Runner.Tests;

/// <summary>
/// Plumbing of <see cref="Rejudge.RunAsync"/> against synthetic results: stored code is
/// re-scored through the supplied judge, original generation cost/latency fields are
/// copied through untouched, lines without stored output or with retired tasks are
/// skipped, and a rerun replaces the output file instead of appending. Judge semantics
/// themselves are covered by CompositeJudgeTests.
/// </summary>
public sealed class RejudgeTests : IDisposable
{
    private static readonly IReadOnlyList<BenchTask> OneTask =
    [
        new("task-a", "async", "prompt", "criteria", "tests"),
    ];

    private sealed class StubJudge(double score) : IJudge
    {
        public JudgeRequest? LastRequest { get; private set; }

        public Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new JudgeVerdict(Passed: true, score, "rejudged"));
        }
    }

    private readonly string _root = Directory.CreateTempSubdirectory("sharpbench-rejudge-").FullName;

    private string ResultsDir => Path.Combine(_root, "results");
    private string RejudgeDir => Path.Combine(_root, "results-rejudge", "stub-judge");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private void WriteResults(params string[] jsonLines)
    {
        Directory.CreateDirectory(ResultsDir);
        File.WriteAllLines(Path.Combine(ResultsDir, "m.jsonl"), jsonLines);
    }

    private static string Line(string task, int generation, double score, string? output)
    {
        var outputField = output is null ? "" : $"\"output\":{JsonSerializer.Serialize(output)},";
        return $"{{\"model\":\"model-x\",\"category\":\"async\",\"task\":\"{task}\"," +
               $"\"generation\":{generation},\"passed\":false,\"score\":{score:0.0#}," +
               $"\"sutLatencyMs\":1234,\"sutInputTokens\":100,\"sutOutputTokens\":200," +
               $"{outputField}\"timestampUtc\":\"2026-07-05T10:00:00Z\"}}";
    }

    private async Task<StubJudge> RunAsync(double score = 0.85)
    {
        var judge = new StubJudge(score);
        await Rejudge.RunAsync(_ => judge, OneTask, ResultsDir, RejudgeDir);
        return judge;
    }

    private JsonElement[] ReadRejudged() =>
        File.ReadLines(Path.Combine(RejudgeDir, "model-x.jsonl"))
            .Select(l => JsonSerializer.Deserialize<JsonElement>(l))
            .ToArray();

    [Fact]
    public async Task Rescores_stored_code_and_copies_the_original_generation_cost_fields()
    {
        WriteResults(Line("task-a", 1, 0.2, output: "class C {}"));

        var judge = await RunAsync(score: 0.85);

        var line = Assert.Single(ReadRejudged());
        Assert.Equal(0.85, line.GetProperty("score").GetDouble());
        Assert.True(line.GetProperty("passed").GetBoolean());
        Assert.Equal("rejudged", line.GetProperty("reasoning").GetString());
        Assert.Equal("class C {}", line.GetProperty("output").GetString());
        // The generation itself is not re-run: its cost and latency carry over verbatim.
        Assert.Equal(1234, line.GetProperty("sutLatencyMs").GetInt32());
        Assert.Equal(100, line.GetProperty("sutInputTokens").GetInt64());
        Assert.Equal(200, line.GetProperty("sutOutputTokens").GetInt64());
        // The judge saw the same request shape EvalRunner builds during a live run.
        Assert.Equal(new JudgeRequest("class C {}", "criteria", "prompt"), judge.LastRequest);
    }

    [Fact]
    public async Task Skips_lines_with_no_stored_output_or_a_retired_task()
    {
        WriteResults(
            Line("task-a", 1, 0.9, output: "class C {}"),
            Line("task-a", 2, 0.9, output: null),
            Line("task-gone", 1, 0.9, output: "class C {}"));

        await RunAsync();

        var line = Assert.Single(ReadRejudged());
        Assert.Equal(1, line.GetProperty("generation").GetInt32());
    }

    [Fact]
    public async Task Rerun_replaces_the_output_file_instead_of_appending()
    {
        WriteResults(Line("task-a", 1, 0.2, output: "class C {}"));

        await RunAsync();
        await RunAsync();

        Assert.Single(ReadRejudged());
    }
}
