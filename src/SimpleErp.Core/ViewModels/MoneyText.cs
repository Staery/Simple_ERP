using System.Globalization;

namespace SimpleErp.Core.ViewModels;

/// <summary>Formats and parses amounts typed into text boxes.</summary>
public static class MoneyText
{
    public static string Format(decimal value, IFormatProvider? culture = null) =>
        value.ToString("#,0.##", culture ?? CultureInfo.CurrentCulture);

    /// <summary>
    /// Parses an amount in the current culture, falling back to the invariant one, so both "6 200,50" (ru-RU)
    /// and "6,200.50" are understood. Spaces and a leading currency sign are ignored.
    /// </summary>
    public static bool TryParse(string? text, out decimal value, IFormatProvider? culture = null)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var cleaned = text.Trim().TrimStart('$', '€', '£', '₽').Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return decimal.TryParse(cleaned, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out value)
            || decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}
