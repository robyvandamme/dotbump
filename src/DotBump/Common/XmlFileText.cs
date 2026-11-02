// Copyright © Roby Van Damme.

using System.Xml;
using System.Xml.Linq;

namespace DotBump.Common;

/// <summary>
/// Locates the raw span of XML element text or attribute values.
/// </summary>
internal static class XmlFileText
{
    internal static (int Start, int Length) GetAttributeValueSpan(
        string text,
        int[] lineStartOffsets,
        XAttribute attribute)
    {
        var lineInfo = (IXmlLineInfo)attribute;
        if (!lineInfo.HasLineInfo())
        {
            return (-1, 0);
        }

        var attributeStart = FileText.GetOffset(lineStartOffsets, lineInfo.LineNumber, lineInfo.LinePosition);
        if (attributeStart < 0 || attributeStart > text.Length)
        {
            return (-1, 0);
        }

        var equalsIndex = text.IndexOf('=', attributeStart);
        if (equalsIndex < 0)
        {
            return (-1, 0);
        }

        var quoteIndex = equalsIndex + 1;
        while (quoteIndex < text.Length && char.IsWhiteSpace(text[quoteIndex]))
        {
            quoteIndex++;
        }

        if (quoteIndex >= text.Length || (text[quoteIndex] != '"' && text[quoteIndex] != '\''))
        {
            return (-1, 0);
        }

        var quote = text[quoteIndex];
        var valueStart = quoteIndex + 1;
        var valueEnd = text.IndexOf(quote, valueStart);

        return valueEnd < 0 ? (-1, 0) : FileText.TrimSpan(text, valueStart, valueEnd);
    }

    internal static (int Start, int Length) GetElementTextSpan(
        string text,
        int[] lineStartOffsets,
        XElement element)
    {
        // Only a single ordinary text node represents the complete value. Comments, CDATA, nested
        // elements and multiple text nodes would make a partial replacement corrupt the value.
        var nodes = element.Nodes().ToList();
        if (nodes.Count != 1
            || nodes[0].NodeType != XmlNodeType.Text
            || nodes[0] is not IXmlLineInfo textLineInfo
            || !textLineInfo.HasLineInfo())
        {
            return (-1, 0);
        }

        var valueStart = FileText.GetOffset(lineStartOffsets, textLineInfo.LineNumber, textLineInfo.LinePosition);
        if (valueStart < 0 || valueStart > text.Length)
        {
            return (-1, 0);
        }

        var valueEnd = text.IndexOf('<', valueStart);

        return valueEnd > valueStart ? FileText.TrimSpan(text, valueStart, valueEnd) : (-1, 0);
    }
}
