using System.Threading.Tasks;
using Xunit;

public class CounterHiddenTests
{
    [Fact]
    public void Starts_at_zero()
    {
        Assert.Equal(0L, new Counter().Value);
    }

    [Fact]
    public void Counts_single_threaded_increments()
    {
        var counter = new Counter();
        for (int i = 0; i < 1000; i++)
            counter.Increment();
        Assert.Equal(1000L, counter.Value);
    }

    [Fact]
    public void Concurrent_increments_are_not_lost()
    {
        // Catches a non-atomic ++ that drops updates under contention.
        var counter = new Counter();
        const int tasks = 8;
        const int perTask = 100_000;

        Parallel.For(0, tasks, _ =>
        {
            for (int i = 0; i < perTask; i++)
                counter.Increment();
        });

        Assert.Equal((long)tasks * perTask, counter.Value);
    }
}
