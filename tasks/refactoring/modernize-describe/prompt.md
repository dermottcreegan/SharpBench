The method below works but is dated. Rewrite the class in idiomatic modern C# (.NET 8 / C# 12) **without changing its behavior** — keep the same public class `Inventory`, the same `Describe` signature, and the same output for every input.

```csharp
public static class Inventory
{
    public static string Describe(int? count, string? label)
    {
        string name;
        if (label == null)
            name = "items";
        else
            name = label;

        if (count == null)
            return "unknown " + name;
        if (count.Value == 0)
            return "no " + name;
        return count.Value.ToString() + " " + name;
    }
}
```
