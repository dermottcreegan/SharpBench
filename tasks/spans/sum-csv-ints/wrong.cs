using System;

// Classic bug: only sums segments terminated by a comma, so the final value after
// the last comma — and a lone value with no comma at all — is silently dropped.
public static class SpanParser
{
    public static int SumCsvInts(ReadOnlySpan<char> csv)
    {
        int sum = 0;
        int comma;
        while ((comma = csv.IndexOf(',')) >= 0)
        {
            sum += int.Parse(csv[..comma]);
            csv = csv[(comma + 1)..];
        }
        return sum;
    }
}
