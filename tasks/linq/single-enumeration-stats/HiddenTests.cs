using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public class StatsHiddenTests
{
    private sealed class CountingSequence : IEnumerable<double>
    {
        private readonly double[] _values;
        public int Enumerations;

        public CountingSequence(params double[] values) => _values = values;

        public IEnumerator<double> GetEnumerator()
        {
            Enumerations++;
            return ((IEnumerable<double>)_values).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void Computes_mean_and_max()
    {
        var (mean, max) = Stats.MeanAndMax(new[] { 2.0, 4.0, 9.0 });
        Assert.Equal(5.0, mean, precision: 10);
        Assert.Equal(9.0, max);
    }

    [Fact]
    public void Single_element_is_both_mean_and_max()
    {
        var (mean, max) = Stats.MeanAndMax(new[] { -3.5 });
        Assert.Equal(-3.5, mean);
        Assert.Equal(-3.5, max);
    }

    [Fact]
    public void All_negative_values_max_is_correct()
    {
        // Catches the classic `max = 0` initialization bug.
        var (_, max) = Stats.MeanAndMax(new[] { -8.0, -2.0, -5.0 });
        Assert.Equal(-2.0, max);
    }

    [Fact]
    public void Enumerates_the_source_exactly_once()
    {
        var source = new CountingSequence(1.0, 2.0, 3.0);
        Stats.MeanAndMax(source);
        Assert.Equal(1, source.Enumerations);
    }

    [Fact]
    public void Empty_sequence_throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Stats.MeanAndMax(Array.Empty<double>()));
    }

    [Fact]
    public void Null_sequence_throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Stats.MeanAndMax(null!));
    }
}
