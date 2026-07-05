using Microsoft.Extensions.AI;
using SharpBench.Runner;

// Usage:
//   dotnet run                          -- list the task inventory (no API keys needed)
//   dotnet run -- --model <label>       -- run the benchmark for one model
//   dotnet run -- --model <label> --runs <n>  -- override generations per task (default 3)
//   dotnet run -- --report              -- markdown leaderboard from results/ (no keys needed)
//
// Model labels: bare frontier IDs (claude-sonnet-5, gpt-4o, gemini-2.5-pro), an
// explicit provider:model prefix (openai:gpt-4o), or ollama:<model>. Keys come from
// ANTHROPIC_API_KEY / OPENAI_API_KEY / GEMINI_API_KEY, either as real environment
// variables or dropped into a repo-root .env file (see .env.example). See ChatClientFactory.

var repoRoot = FindRepoRoot();
LoadDotEnv(repoRoot);
var tasks = TaskLoader.LoadAll(Path.Combine(repoRoot, "tasks"));

if (args.Contains("--report"))
{
    Console.WriteLine(Leaderboard.Render(
        Path.Combine(repoRoot, "results"), tasks, Path.Combine(repoRoot, "pricing.json")));
    return;
}

Console.WriteLine($"SharpBench: {tasks.Count} tasks in {tasks.Select(t => t.Category).Distinct().Count()} categories");
foreach (var group in tasks.GroupBy(t => t.Category))
    Console.WriteLine($"  {group.Key}: {string.Join(", ", group.Select(t => t.Id))}");

var modelIndex = Array.IndexOf(args, "--model");
if (modelIndex < 0 || modelIndex + 1 >= args.Length)
{
    Console.WriteLine("\nDry inventory only. Pass --model <label> to run the benchmark.");
    return;
}

var modelLabel = args[modelIndex + 1];

// Generations per task. 3 is the published protocol (a task passes when at least half
// its generations pass); --runs 1 keeps development smoke runs cheap. Published tables
// should never mix counts — the leaderboard flags models whose counts differ.
var runs = 3;
var runsIndex = Array.IndexOf(args, "--runs");
if (runsIndex >= 0)
{
    if (runsIndex + 1 >= args.Length || !int.TryParse(args[runsIndex + 1], out runs) || runs < 1)
    {
        Console.Error.WriteLine("--runs needs a positive integer (generations per task).");
        return;
    }
}

var contestant = CreateChatClient(modelLabel);
// One fixed judge model for every contestant; publish the judge transcripts with the results.
var idiomJudge = new NetEval.ChatClientJudge(CreateChatClient("judge"));

var resultsDir = Path.Combine(repoRoot, "results", DateTime.UtcNow.ToString("yyyy-MM"));
Directory.CreateDirectory(resultsDir);
// Model labels can contain characters that are invalid in file names (e.g. "ollama:qwen2.5-coder:7b").
var fileLabel = string.Join("-", modelLabel.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
var resultsPath = Path.Combine(resultsDir, $"{fileLabel}.jsonl");

await BenchmarkRun.RunModelAsync(modelLabel, contestant, idiomJudge, tasks, resultsPath, runs);
Console.WriteLine($"\nDone. Raw results: {resultsPath}");

static IChatClient CreateChatClient(string modelLabel)
{
    if (modelLabel == "judge")
    {
        // Fixed judge model for every contestant. Defaults to a frontier Claude model
        // for published results; override with SHARPBENCH_JUDGE_MODEL (e.g. an Ollama
        // model for offline smoke runs).
        var judgeModel = Environment.GetEnvironmentVariable("SHARPBENCH_JUDGE_MODEL") ?? "claude-opus-4-8";
        return ChatClientFactory.Create(judgeModel);
    }

    return ChatClientFactory.Create(modelLabel);
}

static void LoadDotEnv(string repoRoot)
{
    var path = Path.Combine(repoRoot, ".env");
    if (!File.Exists(path))
        return;

    foreach (var line in File.ReadAllLines(path))
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            continue;

        var separator = trimmed.IndexOf('=');
        if (separator < 0)
            continue;

        var key = trimmed[..separator].Trim();
        var value = trimmed[(separator + 1)..].Trim().Trim('"');

        // Real environment variables win over the .env file.
        if (Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}

static string FindRepoRoot()
{
    // Walk up from the working directory until we find the tasks/ folder, so
    // `dotnet run` works from the repo root or the project directory.
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    for (; dir is not null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, "tasks")))
            return dir.FullName;
    throw new DirectoryNotFoundException("Could not find the tasks/ folder above the working directory.");
}
