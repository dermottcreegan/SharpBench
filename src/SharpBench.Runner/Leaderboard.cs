using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpBench.Runner;

/// <summary>One line of a results JSONL file, as written by <see cref="BenchmarkRun"/>.</summary>
public sealed record ResultLine(
    string Model,
    string Category,
    string Task,
    bool Passed,
    double Score,
    int SutLatencyMs,
    long? SutInputTokens,
    long? SutOutputTokens,
    DateTime TimestampUtc);

/// <summary>Per-model list price in dollars per million tokens, one entry of <see cref="PricingFile"/>.</summary>
public sealed record ModelPricing(decimal Input, decimal Output, string? Note);

/// <summary>
/// Shape of the repo-root <c>pricing.json</c>. Prices live in a data file rather than
/// the results JSONL or this code so that (a) a rerun of --report reprices old runs
/// when vendors change rates, and (b) the git history of the file records exactly
/// which rates each published report used.
/// </summary>
public sealed record PricingFile(string AsOf, string Unit, Dictionary<string, ModelPricing> Models);

/// <summary>Builds a markdown leaderboard from the JSONL files under <c>results/</c>.</summary>
public static class Leaderboard
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>Renders the leaderboard for every result under <paramref name="resultsRoot"/>.
    /// When a (model, task) pair appears more than once — reruns append rather than replace —
    /// only the latest line by timestamp counts. <paramref name="pricingPath"/> points at
    /// pricing.json; when the file is absent the cost column renders as "—".</summary>
    public static string Render(string resultsRoot, IReadOnlyList<BenchTask> tasks, string pricingPath)
    {
        var results = LoadLatest(resultsRoot);
        var pricing = LoadPricing(pricingPath);
        if (results.Count == 0)
            return $"No results found under {resultsRoot}. Run with --model <label> first.";

        var taskCount = tasks.Count;
        var categories = tasks.Select(t => t.Category).Distinct().Order().ToList();

        var models =
            (from r in results
             group r by r.Model into byModel
             let passed = byModel.Count(r => r.Passed)
             let meanScore = byModel.Average(r => r.Score)
             orderby meanScore descending, passed descending
             select new
             {
                 Model = byModel.Key,
                 Rows = byModel.ToList(),
                 Passed = passed,
                 MeanScore = meanScore,
                 MeanLatencyMs = byModel.Average(r => r.SutLatencyMs),
                 InputTokens = SumOrNull(byModel, r => r.SutInputTokens),
                 OutputTokens = SumOrNull(byModel, r => r.SutOutputTokens),
             })
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("## Leaderboard");
        sb.AppendLine();
        sb.AppendLine("| Model | Passed | Mean score | Mean latency | Tokens in / out | Cost / run |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var m in models)
        {
            var scored = m.Rows.Count == taskCount ? "" : $" of {m.Rows.Count} run";
            sb.AppendLine(
                $"| {m.Model} | {m.Passed}/{taskCount}{scored} | {m.MeanScore:0.000} | " +
                $"{m.MeanLatencyMs / 1000:0.0} s | {Format(m.InputTokens)} / {Format(m.OutputTokens)} | " +
                $"{FormatCost(pricing, m.Model, m.InputTokens, m.OutputTokens)} |");
        }

        if (pricing is not null)
        {
            sb.AppendLine();
            sb.AppendLine($"*Cost prices the full run at list per-MTok rates from `pricing.json` (as of {pricing.AsOf}).*");
        }

        sb.AppendLine();
        sb.AppendLine("## Mean score by category");
        sb.AppendLine();
        sb.Append("| Category |");
        foreach (var m in models)
            sb.Append($" {m.Model} |");
        sb.AppendLine();
        sb.Append("|---|").AppendLine(string.Concat(Enumerable.Repeat("---|", models.Count)));
        foreach (var category in categories)
        {
            sb.Append($"| {category} |");
            foreach (var m in models)
            {
                var rows = m.Rows.Where(r => r.Category == category).ToList();
                sb.Append(rows.Count == 0 ? " — |" : $" {rows.Average(r => r.Score):0.00} |");
            }
            sb.AppendLine();
        }

        // Failures deserve a callout: the tables above only show aggregates.
        var failures = results.Where(r => !r.Passed).OrderBy(r => r.Model).ThenBy(r => r.Category).ToList();
        if (failures.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Failed tasks");
            sb.AppendLine();
            foreach (var f in failures)
                sb.AppendLine($"- {f.Model}: {f.Category}/{f.Task} (score {f.Score:0.00})");
        }

        return sb.ToString();
    }

    /// <summary>Reads every *.jsonl under <paramref name="resultsRoot"/> (recursively, so monthly
    /// subfolders are picked up) and keeps the latest line per (model, category, task).</summary>
    private static IReadOnlyList<ResultLine> LoadLatest(string resultsRoot)
    {
        if (!Directory.Exists(resultsRoot))
            return [];

        var lines =
            from file in Directory.EnumerateFiles(resultsRoot, "*.jsonl", SearchOption.AllDirectories)
            from line in File.ReadLines(file)
            where !string.IsNullOrWhiteSpace(line)
            select JsonSerializer.Deserialize<ResultLine>(line, JsonOptions)!;

        return lines
            .GroupBy(r => (r.Model, r.Category, r.Task))
            .Select(g => g.OrderBy(r => r.TimestampUtc).Last())
            .ToList();
    }

    private static long? SumOrNull(IEnumerable<ResultLine> rows, Func<ResultLine, long?> selector)
    {
        long sum = 0;
        foreach (var row in rows)
        {
            if (selector(row) is not long value)
                return null; // Provider reported no usage for at least one task.
            sum += value;
        }
        return sum;
    }

    private static string Format(long? tokens) => tokens is long t ? t.ToString("N0") : "—";

    private static PricingFile? LoadPricing(string pricingPath)
    {
        if (!File.Exists(pricingPath))
            return null;

        var pricing = JsonSerializer.Deserialize<PricingFile>(File.ReadAllText(pricingPath), JsonOptions);
        if (pricing is null)
            return null;

        // Rebuild the model map case-insensitively; JSON object keys deserialize case-sensitive.
        var models = new Dictionary<string, ModelPricing>(pricing.Models, StringComparer.OrdinalIgnoreCase);
        return pricing with { Models = models };
    }

    private static string FormatCost(PricingFile? pricing, string model, long? inputTokens, long? outputTokens)
    {
        if (model.StartsWith("ollama:", StringComparison.OrdinalIgnoreCase))
            return "$0 (local)";

        // Explicit provider prefixes ("openai:gpt-4o") price the same as the bare ID.
        var key = model.Split(':', 2) is [_, var bare] ? bare : model;
        if (pricing is null || !pricing.Models.TryGetValue(key, out var rate)
            || inputTokens is not long input || outputTokens is not long output)
            return "—";

        var dollars = (input * rate.Input + output * rate.Output) / 1_000_000m;
        return dollars.ToString("$0.000", System.Globalization.CultureInfo.InvariantCulture);
    }
}
