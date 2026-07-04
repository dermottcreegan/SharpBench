#nullable enable

// Classic bug: a bare ?? chain treats an empty string as a present value, so ""
// is returned instead of falling through to the next candidate.
public static class Config
{
    public static string Resolve(string? primary, string? fallback, string @default)
    {
        return primary ?? fallback ?? @default;
    }
}
