#nullable enable

public static class Config
{
    public static string Resolve(string? primary, string? fallback, string @default)
    {
        if (!string.IsNullOrEmpty(primary)) return primary;
        if (!string.IsNullOrEmpty(fallback)) return fallback;
        return @default;
    }
}
