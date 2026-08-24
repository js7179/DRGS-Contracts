using System.Collections;

namespace DRGSContracts.Terminal;

public readonly struct DateRange : IEnumerable<DateOnly>
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private readonly MissionType _missionType;

    /// <summary>
    /// Creates a DateRange that starts at a specific day and ends at a specific day, inclusive
    /// </summary>
    /// <param name="start">The day we start the range on</param>
    /// <param name="end">The day to stop at in the range, inclusive</param>
    /// <param name="missionType">The type of mission we're scraping, which will affect how many days we jump to the next date in the range</param>
    /// <exception cref="ArgumentException">If end comes before start</exception>
    public DateRange(DateOnly start, DateOnly end, MissionType missionType)
    {
        if(end < start) throw new ArgumentException("End date must be on or after the start date.", nameof(end));
        Start = start;
        End = end;
        _missionType = missionType;
    }

    /// <summary>
    /// Creates a DateRange that starts at a specific day and continues for a given number of iterations
    /// </summary>
    /// <param name="start">The day we start the range on</param>
    /// <param name="occurrences">How many dates we should have in this range</param>
    /// <param name="missionType">The type of mission we're scraping, which will affect how many days we jump to the next date in the range</param>
    /// <exception cref="ArgumentOutOfRangeException"> If occurrences is zero or negative</exception>
    public DateRange(DateOnly start, int occurrences, MissionType missionType)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(occurrences, 1, nameof(occurrences));
        Start = start;
        _missionType = missionType;
        
        End = Start.AddDays((occurrences - 1) * (int)missionType);
    }

    /// <summary>
    /// Enumerates every day in the range specified so the program
    /// may iterate over them
    /// </summary>
    /// <returns>Enumerator of <see cref="System.DateOnly" /> for every possible date in range</returns>
    public IEnumerator<DateOnly> GetEnumerator()
    {
        for(
            var date = Start; 
            date <= End; 
            date = date.AddDays( (int)_missionType )
        )
            yield return date;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}