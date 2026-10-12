using System.Globalization;
using System.Linq;

namespace KludgeBox.Core.Text;

public static class StringExtensions
{
    /// <summary>
    /// Determines whether the text contains no control, format, line separator or paragraph separator characters,
    /// so it stays on one line and hides nothing invisible.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>
    /// <c>true</c> if every character of the text is printable on one line, including for an empty text;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSingleLinePrintable(this string text) =>
        !text.Any(c => char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.Format
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator);

    /// <summary>
    /// Determines whether the text is printable on one line and contains no whitespace, so it reads as one word.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>
    /// <c>true</c> if the text is single-line printable without whitespace, including for an empty text;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool IsSingleWordPrintable(this string text) =>
        text.IsSingleLinePrintable() && !text.Any(char.IsWhiteSpace);
}
