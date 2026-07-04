using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// Classic bug: reduces with no seed, so an empty sequence throws
// InvalidOperationException instead of returning T.Zero.
public static class Aggregate
{
    public static T Sum<T>(IEnumerable<T> values) where T : INumber<T>
    {
        return values.Aggregate((a, b) => a + b);
    }
}
