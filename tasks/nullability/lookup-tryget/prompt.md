With `#nullable enable`, write a `sealed class Lookup` with exactly these public members:

```csharp
public void Add(string key, string value);
public bool TryGet(string key, out string? value);
```

Requirements:

- `Add` associates `value` with `key`; adding an existing key overwrites the previous value.
- `TryGet` sets `value` and returns `true` when `key` is present; otherwise it sets `value` to `null` and returns `false`. It must not throw for a missing key.
- The code compiles clean under `#nullable enable`.
