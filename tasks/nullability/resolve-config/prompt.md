With `#nullable enable`, write a static class named `Config` with exactly this public method:

```csharp
public static string Resolve(string? primary, string? fallback, string @default)
```

Requirements:

- Return the first of `primary`, `fallback` that is neither null nor empty; if neither qualifies, return `@default`.
- An empty string (`""`) counts as a missing value, the same as `null`.
- `@default` is never null and is returned as-is.
- The code compiles clean under `#nullable enable`.
