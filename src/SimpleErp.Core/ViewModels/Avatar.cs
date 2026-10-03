namespace SimpleErp.Core.ViewModels;

/// <summary>Picks one of the avatar colors for a person, stable for the same id.</summary>
public static class Avatar
{
    public const int PaletteSize = 8;

    public static int ColorIndex(Guid id)
    {
        var sum = 0;
        foreach (var b in id.ToByteArray())
        {
            sum += b;
        }

        return sum % PaletteSize;
    }
}
