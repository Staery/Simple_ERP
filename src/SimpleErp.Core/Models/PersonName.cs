namespace SimpleErp.Core.Models;

internal static class PersonName
{
    public static string Initials(string? firstName, string? lastName)
    {
        var first = FirstLetter(firstName);
        var last = FirstLetter(lastName);
        var initials = $"{first}{last}";
        return initials.Length == 0 ? "?" : initials;
    }

    private static string FirstLetter(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? string.Empty : char.ToUpperInvariant(trimmed[0]).ToString();
    }
}
