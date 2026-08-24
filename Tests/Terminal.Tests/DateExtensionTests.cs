namespace Terminal.Tests;

using DRGSContracts.Terminal;

public class DateExtensionTests
{
    [Test]
    [Arguments(2026, 8, 3, "Monday")]
    [Arguments(2026, 8, 4, "Tuesday")]
    [Arguments(2026, 8, 5, "Wednesday")]
    [Arguments(2026, 8, 6, "Thursday")]
    [Arguments(2026, 8, 7, "Friday")]
    [Arguments(2026, 8, 8, "Saturday")]
    [Arguments(2026, 8, 9, "Sunday")]
    public async Task MostRecentMonday_ReturnsMondayOfSameWeek(int year, int month, int day, string dayName)
    {
        var date = new DateOnly(year, month, day);
        var expectedMonday = new DateOnly(2026, 8, 3); // Monday, August 3rd, 2026
        
        await Assert.That(date.MostRecentMonday()).IsEqualTo(expectedMonday);
    }
    
}