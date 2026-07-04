using System.Threading.Channels;
using System.Threading.Tasks;

// Classic bug: the producer never completes the writer, so the consumer's
// ReadAllAsync waits forever for more items and the method never returns.
public static class Pipeline
{
    public static async Task<long> ProduceConsumeSumAsync(int count)
    {
        var channel = Channel.CreateBounded<int>(16);

        var producer = Task.Run(async () =>
        {
            for (int i = 0; i < count; i++)
                await channel.Writer.WriteAsync(i);
            // Missing: channel.Writer.Complete();
        });

        long sum = 0;
        await foreach (var value in channel.Reader.ReadAllAsync())
            sum += value;

        await producer;
        return sum;
    }
}
