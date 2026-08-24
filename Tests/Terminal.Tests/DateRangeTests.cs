using DRGSContracts.Terminal;

namespace Terminal.Tests;

public class DateRangeTests
{
    [Test]
    public async Task DateRange_StartEnd_Error()
    {
        var start = new DateOnly(2026, 8, 4); // Tuesday, August 4th, 2026
        var end = new DateOnly(2026, 8, 3); // Monday, August 3rd, 2026
        
        Assert.Throws<ArgumentException>(() => { _ = new DateRange(start, end, MissionType.VanguardContract); });
    }

    [Test]
    public async Task DateRange_StartEnd_OneDay()
    {
        var date = new DateOnly(2026, 8, 4); // Tuesday, August 4th, 2026
        
        var range = new DateRange(date, date, MissionType.VanguardContract);
        await Assert.That(range.Start).IsEqualTo(date);
        await Assert.That(range.End).IsEqualTo(date);

        var days = range.ToList();
        await Assert.That(days).IsEquivalentTo([date]);
    }

    [Test]
    public async Task DateRange_StartEnd_Lethal_TwoWeek()
    {
        var start = new DateOnly(2026, 8, 3); // Monday, August 3rd, 2026
        var end = new DateOnly(2026, 8, 16); // Sunday, August 16th, 2026

        var range = new DateRange(start, end, MissionType.LethalOperation);
        await Assert.That(range.Start).IsEqualTo(start);
        await Assert.That(range.End).IsEqualTo(end);
        
        var days = range.ToList();
        await Assert.That(days).IsEquivalentTo([start, start.AddDays(7)]);
    }

    [Test]
    public async Task DateRange_StartOccurrence_Error()
    {
        var start = new DateOnly(2026, 8, 3); // Monday, August 3rd, 2026

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new DateRange(start, 0, MissionType.VanguardContract); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new DateRange(start, -1, MissionType.VanguardContract); });
    }

    [Test]
    public async Task DateRange_StartOccurrence_OneOff()
    {
        var start = new DateOnly(2026, 8, 3); // Monday, August 3rd, 2026
        
        var range = new DateRange(start, 1, MissionType.VanguardContract);
        await Assert.That(range.Start).IsEqualTo(start);
        await Assert.That(range.End).IsEqualTo(start);
        
        var days = range.ToList();
        await Assert.That(days).IsEquivalentTo([start]);
    }

    [Test]
    public async Task DateRange_StartOccurrence_Lethal()
    {
        var start = new DateOnly(2026, 8, 3);

        var range = new DateRange(start, 3, MissionType.LethalOperation);

        var expectedEnd = start.AddDays(14);
        await Assert.That(range.Start).IsEqualTo(start);
        await Assert.That(range.End).IsEqualTo(expectedEnd);
        
        var days = range.ToList();
        await Assert.That(days).IsEquivalentTo([start, start.AddDays(7), start.AddDays(14)]);
    }

    [Test]
    public async Task DateRange_Vanguard_Week()
    {
        var start = new DateOnly(2026, 8, 1);
        var end = new DateOnly(2026, 8, 7);
        var range = new DateRange(start, end, MissionType.VanguardContract);
        
        await Assert.That(range.Start).IsEqualTo(start);
        await Assert.That(range.End).IsEqualTo(end);
        var days = range.ToList();
        
        await Assert.That(days).IsEquivalentTo([
            start.AddDays(0),
            start.AddDays(1),
            start.AddDays(2),
            start.AddDays(3),
            start.AddDays(4),
            start.AddDays(5),
            start.AddDays(6),
        ]);
    }
}