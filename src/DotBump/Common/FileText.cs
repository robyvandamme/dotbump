// Copyright © Roby Van Damme.

using System.Text;

namespace DotBump.Common;

/// <summary>
/// Reads and writes text files while preserving their encoding, byte order mark and line endings, and
/// provides line and span offset helpers.
/// </summary>
internal static class FileText
{
    internal static string ReadText(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        var encoding = DetectEncoding(bytes);
        var preambleLength = encoding.GetPreamble().Length;

        return encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
    }

    internal static void WriteText(string filePath, string text)
    {
        // The encoding is detected from the original bytes and carries the BOM, so the file is
        // written back with the same encoding and byte order mark it had before.
        var encoding = DetectEncoding(File.ReadAllBytes(filePath));
        File.WriteAllText(filePath, text, encoding);
    }

    internal static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return new UnicodeEncoding(false, true);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return new UnicodeEncoding(true, true);
        }

        return new UTF8Encoding(false);
    }

    internal static int[] GetLineStartOffsets(string text)
    {
        var offsets = new List<int> { 0 };

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            var isLineBreak = character == '\n'
                              || (character == '\r' && (i + 1 >= text.Length || text[i + 1] != '\n'));

            if (isLineBreak)
            {
                offsets.Add(i + 1);
            }
        }

        return offsets.ToArray();
    }

    internal static int GetOffset(int[] lineStartOffsets, int lineNumber, int linePosition)
    {
        var lineIndex = lineNumber - 1;
        if (lineIndex < 0 || lineIndex >= lineStartOffsets.Length)
        {
            return -1;
        }

        return lineStartOffsets[lineIndex] + (linePosition - 1);
    }

    internal static (int Start, int Length) TrimSpan(string text, int start, int end)
    {
        while (start < end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(text[end - 1]))
        {
            end--;
        }

        return end > start ? (start, end - start) : (-1, 0);
    }
}
