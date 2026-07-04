namespace SharpBench.Judging.Tests;

/// <summary>Locates and reads files from the repo's <c>tasks/</c> tree (found by walking up
/// from the test output directory), shared by the judge-mechanics and task-contract tests.</summary>
internal static class TaskFiles
{
    public static string TasksRoot { get; } = FindTasksRoot();

    public static string Read(string category, string id, string file)
    {
        var path = Path.Combine(TasksRoot, category, id, file);
        if (!File.Exists(path))
            throw new FileNotFoundException($"tasks/{category}/{id}/{file} not found.", path);
        return File.ReadAllText(path);
    }

    private static string FindTasksRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var tasks = Path.Combine(dir.FullName, "tasks");
            if (Directory.Exists(tasks))
                return tasks;
        }
        throw new DirectoryNotFoundException($"tasks/ not found above {AppContext.BaseDirectory}.");
    }
}
