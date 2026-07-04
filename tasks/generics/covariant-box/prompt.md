Define exactly these types:

```csharp
public interface IReadOnlyBox<out T>
{
    T Value { get; }
}

public sealed class Box<T> : IReadOnlyBox<T>
{
    public Box(T value);
    public T Value { get; }
}
```

Requirements:

- `Box<T>` stores the value passed to its constructor and exposes it through `Value`.
- The interface must be covariant in `T`, so an `IReadOnlyBox<string>` can be used where an `IReadOnlyBox<object>` is expected.
