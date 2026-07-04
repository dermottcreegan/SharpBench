using System.Threading;

public sealed class Counter
{
    private long _value;

    public void Increment() => Interlocked.Increment(ref _value);

    public long Value => Interlocked.Read(ref _value);
}
