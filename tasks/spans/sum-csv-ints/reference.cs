using System;

public static class SpanParser
{
    public static int SumCsvInts(ReadOnlySpan<char> csv)
    {
        int sum = 0;
        while (!csv.IsEmpty)
        {
            int comma = csv.IndexOf(',');
            ReadOnlySpan<char> segment = comma < 0 ? csv : csv[..comma];
            sum += int.Parse(segment);
            csv = comma < 0 ? ReadOnlySpan<char>.Empty : csv[(comma + 1)..];
        }
        return sum;
    }
}
