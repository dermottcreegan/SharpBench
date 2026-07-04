using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using NetEval;

namespace SharpBench.Judging;

/// <summary>
/// The objective layer of SharpBench: scores the fraction of a task's hidden xUnit
/// tests that the model's code passes (<c>Passed</c> requires all of them).
///
/// Construct one instance per benchmark task (each task has its own hidden tests).
///
/// The compile-and-run happens in a disposable <c>SharpBench.Sandbox</c> child
/// process, not in-process: model-generated code is untrusted, so this orchestrator
/// launches the sandbox, feeds it the candidate + hidden tests over stdin, and
/// enforces a hard wall-clock timeout. On breach it kills the whole process tree —
/// the only thing that reliably stops <c>Environment.Exit()</c>, infinite loops,
/// and leaked threads. The parent process is never at their mercy.
/// </summary>
public sealed class RoslynTestJudge(string hiddenTestsSource, TimeSpan? hardTimeout = null) : IJudge
{
    private readonly TimeSpan _hardTimeout = hardTimeout ?? TimeSpan.FromSeconds(60);

    public async Task<JudgeVerdict> JudgeAsync(JudgeRequest request, CancellationToken cancellationToken = default)
    {
        var candidateCode = CodeExtractor.ExtractCSharp(request.Output);
        var requestJson = JsonSerializer.Serialize(new SandboxRequest(candidateCode, hiddenTestsSource));

        var sandboxPath = ResolveSandboxPath();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                ArgumentList = { "exec", sandboxPath },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();

        // Read stdout/stderr concurrently with writing stdin to avoid pipe deadlock.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.StandardInput.WriteAsync(requestJson.AsMemory(), cancellationToken);
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_hardTimeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillTree(process);
            return new JudgeVerdict(false, 0.0,
                $"Candidate sandbox exceeded the {_hardTimeout.TotalSeconds:0}s limit and was killed.");
        }
        catch (OperationCanceledException)
        {
            KillTree(process);
            throw;
        }

        var stdout = await stdoutTask;
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            var stderr = (await stderrTask).Trim();
            return new JudgeVerdict(false, 0.0,
                $"Candidate sandbox exited {process.ExitCode} without a verdict. {Truncate(stderr, 300)}".Trim());
        }

        try
        {
            var result = JsonSerializer.Deserialize<SandboxResponse>(stdout)
                         ?? throw new InvalidOperationException("null response");
            return new JudgeVerdict(result.Passed, result.Score, result.Reasoning);
        }
        catch (Exception ex)
        {
            return new JudgeVerdict(false, 0.0,
                $"Candidate sandbox returned an unparsable verdict: {ex.Message}. {Truncate(stdout, 300)}");
        }
    }

    /// <summary>
    /// Locates the built <c>SharpBench.Sandbox.dll</c>. The sandbox is launched with
    /// <c>dotnet exec</c>, which needs the sandbox's own <c>.deps.json</c> and
    /// <c>.runtimeconfig.json</c> beside the dll — so we point at the sandbox's own
    /// build output rather than a copy in the host's bin. Projects that use this
    /// judge reference the sandbox only to force build ordering.
    /// Candidates, in order: <c>SHARPBENCH_SANDBOX_PATH</c>, a copy beside the host
    /// (published-together deployments), then the sandbox's own bin under the repo.
    /// </summary>
    private static string ResolveSandboxPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("SHARPBENCH_SANDBOX_PATH");
        if (!string.IsNullOrWhiteSpace(overridePath))
            return overridePath;

        var beside = Path.Combine(AppContext.BaseDirectory, "SharpBench.Sandbox.dll");
        if (File.Exists(beside))
            return beside;

        var baseDir = AppContext.BaseDirectory;
        var configuration = baseDir.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}")
            ? "Release"
            : "Debug";
        for (var dir = new DirectoryInfo(baseDir); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "SharpBench.sln")))
                continue;
            var inSandboxBin = Path.Combine(
                dir.FullName, "src", "SharpBench.Sandbox", "bin", configuration, "net8.0", "SharpBench.Sandbox.dll");
            if (File.Exists(inSandboxBin))
                return inSandboxBin;
            break;
        }

        throw new FileNotFoundException(
            "SharpBench.Sandbox.dll not found. Build the SharpBench.Sandbox project (the runner and test " +
            "projects reference it, so a solution build produces it), or set SHARPBENCH_SANDBOX_PATH.", beside);
    }

    private static void KillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the HasExited check and Kill — nothing to do.
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    private sealed record SandboxRequest(
        [property: JsonPropertyName("candidateCode")] string CandidateCode,
        [property: JsonPropertyName("hiddenTestsSource")] string HiddenTestsSource);

    private sealed record SandboxResponse(
        [property: JsonPropertyName("passed")] bool Passed,
        [property: JsonPropertyName("score")] double Score,
        [property: JsonPropertyName("reasoning")] string Reasoning);
}
