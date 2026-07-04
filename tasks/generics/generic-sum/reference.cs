using System.Collections.Generic;
using System.Numerics;

public static class Aggregate
{
    public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in values)
            sum += value;
        return sum;
    }
}
