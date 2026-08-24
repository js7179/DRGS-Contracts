namespace DRGSContracts.Terminal;

public static class DateExtensions
{
    /// <summary>
    /// Retrieves the most recent Monday for a given date. If the date is already
    /// Monday, then the date itself is returned.
    /// </summary>
    /// <param name="date">The date we are adjusting from</param>
    /// <returns>The most recent Monday for that given date</returns>
    /// <example>
    /// Saturday, August 1st 2026 => Monday, July 27th 2026
    /// Friday, January 1st 2027 => Monday, December 28th 2026
    /// Monday, August 24th 2026 => Monday, August 24th 2026
    /// </example>
    public static DateOnly MostRecentMonday(this DateOnly date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}