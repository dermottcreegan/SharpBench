using System.Threading.Tasks;
using Xunit;

public class PipelineHiddenTests
{
    [Fact]
    public async Task Sums_produced_values()
    {
        // Catches never completing the writer, which hangs the consumer forever.
        Assert.Equal(499500L, await Pipeline.ProduceConsumeSumAsync(1000));
    }

    [Fact]
    public async Task Zero_count_is_zero()
    {
        Assert.Equal(0L, await Pipeline.ProduceConsumeSumAsync(0));
    }

    [Fact]
    public async Task Sums_more_than_the_channel_capacity()
    {
        Assert.Equal(12497500L, await Pipeline.ProduceConsumeSumAsync(5000));
    }
}
