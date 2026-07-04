using System;
using System.Collections.Generic;
using System.Linq;

// Classic bug: double enumeration. Average() and Max() each walk the sequence,
// so a non-replayable source is consumed twice (and buffered work is wasted).
public static class Stats
{
    public static (double Mean, double Max) MeanAndMax(IEnumerable<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return (values.Average(), values.Max());
    }
}
