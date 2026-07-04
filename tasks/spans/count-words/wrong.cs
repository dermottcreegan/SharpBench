using System;

// Classic bug: counts words as (number of space characters) + 1, mirroring a naive
// text.ToString().Split(' ').Length. Repeated spaces, edge whitespace, and empty
// input all inflate the count.
public static class TextScan
{
    public static int CountWords(ReadOnlySpan<char> text)
    {
        int segments = 1;
        foreach (var c in text)
            if (c == ' ')
                segments++;
        return segments;
    }
}
