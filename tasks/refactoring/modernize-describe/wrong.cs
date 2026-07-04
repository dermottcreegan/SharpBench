// Classic bug: a refactor that changes behavior. Coalescing count to 0 makes a
// null (unknown) count report as "no items", losing the null/zero distinction.
public static class Inventory
{
    public static string Describe(int? count, string? label)
    {
        var name = label ?? "items";
        return (count ?? 0) switch
        {
            0 => $"no {name}",
            _ => $"{count} {name}",
        };
    }
}
