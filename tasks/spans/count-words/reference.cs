using System;

public static class TextScan
{
    public static int CountWords(ReadOnlySpan<char> text)
    {
        int words = 0;
        bool inWord = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                words++;
            }
        }
        return words;
    }
}
