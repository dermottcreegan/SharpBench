public static class Inventory
{
    public static string Describe(int? count, string? label)
    {
        var name = label ?? "items";
        return count switch
        {
            null => $"unknown {name}",
            0 => $"no {name}",
            _ => $"{count} {name}",
        };
    }
}
