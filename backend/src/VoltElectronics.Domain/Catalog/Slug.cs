using System.Text;
using System.Text.RegularExpressions;

namespace VoltElectronics.Domain.Catalog;

/// <summary>URL identity rules for catalog aggregates.</summary>
public static partial class Slug
{
    /// <summary>
    /// Empty in, empty out. Callers derive a provisional slug before the aggregate has vetted the
    /// name it came from, so this must tolerate a name that's about to be rejected.
    /// </summary>
    public static string From(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var lowered = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsAsciiLetterOrDigit(c)) sb.Append(c);
            else if (c is ' ' or '-' or '_') sb.Append('-');
        }
        return Collapse().Replace(sb.ToString(), "-").Trim('-');
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex Collapse();
}
