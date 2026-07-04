using NetEval;
using SharpBench.Judging;
using Xunit;

namespace SharpBench.Judging.Tests;

/// <summary>
/// The gate every benchmark task must pass: its hidden tests must accept a correct
/// reference solution and reject a classic-wrong one. Any task folder that provides
/// both <c>reference.cs</c> and <c>wrong.cs</c> is validated here (through the real
/// sandbox), so a malformed task — hidden tests that pass a broken answer or fail a
/// good one — fails CI instead of silently skewing the benchmark.
/// </summary>
public class TaskContractTests
{
    public static IEnumerable<object[]> ContractTasks()
    {
        foreach (var categoryDir in Directory.EnumerateDirectories(TaskFiles.TasksRoot).Order())
        foreach (var taskDir in Directory.EnumerateDirectories(categoryDir).Order())
        {
            var hasReference = File.Exists(Path.Combine(taskDir, "reference.cs"));
            var hasWrong = File.Exists(Path.Combine(taskDir, "wrong.cs"));
            if (hasReference && hasWrong)
                yield return [Path.GetFileName(categoryDir), Path.GetFileName(taskDir)];
        }
    }

    [Theory]
    [MemberData(nameof(ContractTasks))]
    public async Task Reference_solution_passes_and_wrong_solution_fails(string category, string id)
    {
        var judge = new RoslynTestJudge(TaskFiles.Read(category, id, "HiddenTests.cs"));

        var reference = await judge.JudgeAsync(
            new JudgeRequest(TaskFiles.Read(category, id, "reference.cs"), Criteria: "unused"));
        Assert.True(reference.Passed,
            $"{category}/{id}: reference.cs must pass every hidden test but did not — {reference.Reasoning}");
        Assert.Equal(1.0, reference.Score);

        var wrong = await judge.JudgeAsync(
            new JudgeRequest(TaskFiles.Read(category, id, "wrong.cs"), Criteria: "unused"));
        Assert.False(wrong.Passed,
            $"{category}/{id}: wrong.cs passed every hidden test — the hidden tests do not catch the bug it models.");
        Assert.True(wrong.Score < 1.0,
            $"{category}/{id}: wrong.cs scored {wrong.Score:0.00}; it must fail at least one hidden test.");
    }
}
