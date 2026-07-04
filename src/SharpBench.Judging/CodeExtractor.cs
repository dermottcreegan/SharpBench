using System.Text.RegularExpressions;

namespace SharpBench.Judging;

/// <summary>
/// Pulls compilable C# out of a model response. The system prompt asks for a bare
/// file with no fences, but models add them anyway; a benchmark should not fail a
/// model on formatting when the task is code quality.
/// </summary>
public static partial class CodeExtractor
{
    [GeneratedRegex(@"```(?:csharp|cs|c#)?\s*\n(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex FencedBlock();

    public static string ExtractCSharp(string response)
    {
        var blocks = FencedBlock().Matches(response);
        if (blocks.Count == 0)
            return response.Trim();

        // Take the largest fenced block: models sometimes emit small usage snippets
        // alongside the real answer.
        return blocks.Select(m => m.Groups[1].Value).MaxBy(b => b.Length)!.Trim();
    }
}
