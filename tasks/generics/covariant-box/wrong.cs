// Classic bug: the type parameter is invariant (no `out`), so IReadOnlyBox<string>
// cannot be used as IReadOnlyBox<object>. The covariant assignment in the hidden
// tests then fails to compile.
public interface IReadOnlyBox<T>
{
    T Value { get; }
}

public sealed class Box<T> : IReadOnlyBox<T>
{
    public Box(T value) => Value = value;
    public T Value { get; }
}
