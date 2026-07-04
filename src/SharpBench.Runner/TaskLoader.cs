namespace SharpBench.Runner;

/// <summary>One benchmark task, loaded from a <c>tasks/&lt;category&gt;/&lt;id&gt;/</c> folder.</summary>
/// <param name="Id">Folder name, unique within its category (e.g. "cancellation-propagation").</param>
/// <param name="Category">Parent folder name (e.g. "async").</param>
/// <param name="Prompt">Contents of prompt.md — what the model is asked to write.</param>
/// <param name="Criteria">Contents of criteria.md — idiom rubric for the LLM judge layer.</param>
/// <param name="HiddenTestsSource">Contents of HiddenTests.cs — xUnit tests for the Roslyn judge layer.</param>
public sealed record BenchTask(string Id, string Category, string Prompt, string Criteria, string HiddenTestsSource);

public static class TaskLoader
{
    /// <summary>Loads every task under <paramref name="tasksRoot"/>. A task folder must contain
    /// prompt.md, criteria.md, and HiddenTests.cs; anything else in the folder is ignored.</summary>
    public static IReadOnlyList<BenchTask> LoadAll(string tasksRoot)
    {
        if (!Directory.Exists(tasksRoot))
            throw new DirectoryNotFoundException($"Tasks directory not found: {tasksRoot}");

        var tasks =
            (from categoryDir in Directory.EnumerateDirectories(tasksRoot).Order()
             from taskDir in Directory.EnumerateDirectories(categoryDir).Order()
             select Load(taskDir, Path.GetFileName(categoryDir)))
            .ToList();

        if (tasks.Count == 0)
            throw new InvalidOperationException($"No tasks found under {tasksRoot}.");
        return tasks;
    }

    private static BenchTask Load(string taskDir, string category) =>
        new(
            Id: Path.GetFileName(taskDir),
            Category: category,
            Prompt: ReadRequired(taskDir, "prompt.md"),
            Criteria: ReadRequired(taskDir, "criteria.md"),
            HiddenTestsSource: ReadRequired(taskDir, "HiddenTests.cs"));

    private static string ReadRequired(string taskDir, string fileName)
    {
        var path = Path.Combine(taskDir, fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Task '{Path.GetFileName(taskDir)}' is missing {fileName}.", path);
        return File.ReadAllText(path).Trim();
    }
}
