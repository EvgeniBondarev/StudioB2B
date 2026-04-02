using System.Text.RegularExpressions;
using StudioB2B.Shared;

namespace StudioB2B.Web.Helpers;

/// <summary>
/// Отправления вида <c>0142726576-0093</c>, <c>44716044-2983-1</c> и артикулы вида <c>6631109LMLDEM=DEP</c>, <c>IQ20TT=DNS[4]</c> в тексте чата.
/// </summary>
public static partial class OzonChatShipmentNumberExtractor
{
    /// <summary>7–14 цифр, дефис, 3–4 цифры, опционально суффикс -N.</summary>
    [GeneratedRegex(@"\b\d{7,14}-\d{3,4}(?:-\d+)?\b")]
    private static partial Regex ShipmentPattern();

    /// <summary>Слева от = минимум 4 символа букв/цифр; справа буквы/цифры и опционально [число].</summary>
    [GeneratedRegex(@"\b[A-Za-z0-9]{4,}=[A-Za-z0-9]+(?:\[\d+\])?\b")]
    private static partial Regex ArticlePattern();

    public static List<string> ExtractShipmentsDistinctOrdered(IReadOnlyList<OzonChatMessageDto> messages, Func<string, bool> isFileContent)
    {
        return ExtractByRegex(messages, isFileContent, ShipmentPattern());
    }

    public static List<string> ExtractArticlesDistinctOrdered(IReadOnlyList<OzonChatMessageDto> messages, Func<string, bool> isFileContent)
    {
        return ExtractByRegex(messages, isFileContent, ArticlePattern());
    }

    private static List<string> ExtractByRegex(
        IReadOnlyList<OzonChatMessageDto> messages,
        Func<string, bool> isFileContent,
        Regex pattern)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();

        foreach (var msg in messages)
        {
            if (msg.IsImage) continue;

            foreach (var d in msg.Data)
            {
                if (string.IsNullOrWhiteSpace(d) || isFileContent(d)) continue;

                foreach (Match m in pattern.Matches(d))
                {
                    if (seen.Add(m.Value)) list.Add(m.Value);
                }
            }
        }

        return list;
    }
}
