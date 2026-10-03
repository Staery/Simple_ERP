namespace SimpleErp.Core.Models;

internal static class Dates
{
    /// <summary>Number of full years from <paramref name="from"/> to <paramref name="to"/>; negative if <paramref name="to"/> is earlier.</summary>
    public static int WholeYearsBetween(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            return -WholeYearsBetween(to, from);
        }

        var years = to.Year - from.Year;

        // Not yet reached the anniversary this year. A 29 February birthday counts from 1 March in common years.
        if (to.Month < from.Month || (to.Month == from.Month && to.Day < from.Day))
        {
            years--;
        }

        return years;
    }
}
