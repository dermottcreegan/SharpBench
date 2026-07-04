using System;
using System.Collections.Generic;

public static class Stats
{
    public static (double Mean, double Max) MeanAndMax(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        double sum = 0;
        double max = double.NegativeInfinity;
        long count = 0;
        foreach (var value in values)
        {
            sum += value;
            count++;
            if (value > max) max = value;
        }
        if (count == 0)
            throw new ArgumentException("Sequence contains no elements.", nameof(values));
        return (sum / count, max);
    }
}
