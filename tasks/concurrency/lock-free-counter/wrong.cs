// Classic bug: a plain non-atomic increment. Concurrent callers read, add, and
// write back the same value, so updates are lost under contention.
public sealed class Counter
{
    private long _value;

    public void Increment() => _value++;

    public long Value => _value;
}
