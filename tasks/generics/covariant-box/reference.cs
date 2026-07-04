public interface IReadOnlyBox<out T>
{
    T Value { get; }
}

public sealed class Box<T> : IReadOnlyBox<T>
{
    public Box(T value) => Value = value;
    public T Value { get; }
}
