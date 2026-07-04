using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// SharpBench.Sandbox — the disposable execution half of the objective judge.
//
// Reads one Request as JSON from stdin, compiles the candidate together with the
// hidden xUnit test file, runs every [Fact], and writes one Response as JSON to
// stdout. Nothing else is written to stdout, so the parent can parse it cleanly;
// diagnostics go to stderr.
//
// This process runs untrusted, model-generated code. It is meant to be launched
// per candidate and killed (with its whole process tree) by the parent on a hard
// wall-clock timeout — that outer kill, not anything in here, is what stops
// Environment.Exit(), infinite loops, and leaked threads.

var input = await Console.In.ReadToEndAsync();
Request request;
try
{
    request = JsonSerializer.Deserialize<Request>(input)
              ?? throw new InvalidOperationException("null request");
}
catch (Exception ex)
{
    WriteResponse(new Response(false, 0.0, $"Sandbox could not parse its request: {ex.Message}"));
    return 0;
}

var response = await Sandbox.RunAsync(request.CandidateCode, request.HiddenTestsSource);
WriteResponse(response);
return 0;

static void WriteResponse(Response response)
{
    Console.Out.Write(JsonSerializer.Serialize(response));
    Console.Out.Flush();
}

internal sealed record Request(
    [property: JsonPropertyName("candidateCode")] string CandidateCode,
    [property: JsonPropertyName("hiddenTestsSource")] string HiddenTestsSource);

internal sealed record Response(
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("reasoning")] string Reasoning);

internal static class Sandbox
{
    private static readonly TimeSpan PerTestTimeout = TimeSpan.FromSeconds(10);

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
    [
        .. Net80.References.All,
        MetadataReference.CreateFromFile(typeof(Xunit.Assert).Assembly.Location),        // xunit.assert
        MetadataReference.CreateFromFile(typeof(Xunit.FactAttribute).Assembly.Location), // xunit.core
    ]);

    public static async Task<Response> RunAsync(string candidateCode, string hiddenTestsSource)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create(
            assemblyName: $"SharpBench.Candidate.{Guid.NewGuid():N}",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(candidateCode, parseOptions, path: "Candidate.cs"),
                CSharpSyntaxTree.ParseText(hiddenTestsSource, parseOptions, path: "HiddenTests.cs"),
            ],
            references: References.Value,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var assemblyStream = new MemoryStream();
        var emit = compilation.Emit(assemblyStream);
        if (!emit.Success)
            return new Response(false, 0.0, DescribeCompilationFailure(emit));

        assemblyStream.Position = 0;
        var loadContext = new AssemblyLoadContext($"sharpbench-{Guid.NewGuid():N}", isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(assemblyStream);
            return await RunFactsAsync(assembly);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static string DescribeCompilationFailure(Microsoft.CodeAnalysis.Emit.EmitResult emit)
    {
        var errors = emit.Diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Take(5)
            .Select(d => d.ToString());
        return "Compilation failed: " + string.Join(" | ", errors);
    }

    private static async Task<Response> RunFactsAsync(Assembly assembly)
    {
        var facts =
            (from type in assembly.GetTypes()
             where type is { IsClass: true, IsAbstract: false }
             from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
             where method.GetCustomAttributes().Any(a => a.GetType().FullName == "Xunit.FactAttribute")
             select (Type: type, Method: method))
            .ToList();

        if (facts.Count == 0)
            return new Response(false, 0.0,
                "No [Fact] tests were discovered in the compiled assembly — hidden test file is malformed.");

        var passed = 0;
        var failures = new StringBuilder();
        foreach (var (type, method) in facts)
        {
            var failure = await RunSingleFactAsync(type, method);
            if (failure is null)
                passed++;
            else
                failures.Append($" | {method.Name}: {failure}");
        }

        var score = (double)passed / facts.Count;
        var reasoning = passed == facts.Count
            ? $"All {facts.Count} hidden tests passed."
            : $"{passed}/{facts.Count} hidden tests passed.{failures}";
        return new Response(passed == facts.Count, score, reasoning);
    }

    /// <summary>Returns null when the fact passes, otherwise a short failure description.</summary>
    private static async Task<string?> RunSingleFactAsync(Type type, MethodInfo method)
    {
        // Task.Run so a synchronously-hanging candidate only leaks a thread pool
        // thread instead of wedging this process before the parent's kill lands.
        var execution = Task.Run(async () =>
        {
            object? instance = method.IsStatic ? null : Activator.CreateInstance(type);
            try
            {
                var result = method.Invoke(instance, null);
                if (result is Task task)
                    await task;
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }
        });

        try
        {
            var finished = await Task.WhenAny(execution, Task.Delay(PerTestTimeout));
            if (finished != execution)
                return $"timed out after {PerTestTimeout.TotalSeconds:0}s";
            await execution;
            return null;
        }
        catch (Exception ex)
        {
            var actual = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
            return Truncate($"{actual.GetType().Name}: {actual.Message}", 200);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
