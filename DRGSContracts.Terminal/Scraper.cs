using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.Globalization;
using DRGSContracts.Terminal.DisplayCapture;
using DRGSContracts.Terminal.GameBridge;
using ImageMagick;
using Sharprompt;

namespace DRGSContracts.Terminal;

internal static class Scraper
{
    /// <summary>
    /// <see cref="System.TimeOnly" /> pointing to just after the 06:00 UTC reset - buffer giving some headroom after reset
    /// </summary>
    private static readonly TimeOnly ResetTimeWithBuffer = new(6, 5);

    internal static void Scrape(MissionType missionType, int gpuIndex, int displayIndex, DirectoryInfo outputFolder)
    {
        var range = PromptForRange(missionType);
        string prefix = missionType switch
        {
            MissionType.VanguardContract => "vanguard",
            MissionType.LethalOperation => "lethal",
            _ => throw new ArgumentOutOfRangeException(nameof(missionType), missionType, "Unknown mission type"),
        };
        using var displayCaptureSession = new DisplayCaptureSession(gpuIndex, displayIndex);
        foreach (var date in range)
        {
            // There's an issue I had with the game not showing subsequent LO information after the first override that
            // is fed into the hook. After the scrape was done, it updated back to this week's LO information since the
            // override was cleared. Hence, this is a workaround to make it show subsequent LO information after the initial
            // one in the date range. This issue wasn't present for VCs, so we only do this for LOs. The flashing of this week's
            // LO after every step is normal and accepted behavior considering the 50ms delay to allow the game time to make
            // a new GetSystemTimeAsFileTime() call to update back to this week's LO.
            if (missionType == MissionType.LethalOperation)
            {
                PipeController.ClearTimeOverride();
                Thread.Sleep(50);
            }
            var targetDto = new DateTimeOffset(date, ResetTimeWithBuffer, TimeSpan.Zero);
            PipeController.SendDate(targetDto);
            
            string targetDtoDisplay = targetDto.ToString("yyyy-MM-dd");
            Console.Write($"Game's system clock set to {targetDtoDisplay}, press ENTER to capture: ");
            Console.ReadLine(); // await for user to capture
            
            var frameCapture = displayCaptureSession.CaptureDisplay();
            var scrapedAt = DateTimeOffset.Now.ToUniversalTime();
            
            using var memoryStream = new MemoryStream();
            frameCapture.Save(memoryStream, ImageFormat.Png);
            memoryStream.Position = 0;
            
            using var outputImage = new MagickImage(memoryStream);

            var exifProfile = new ExifProfile();
            exifProfile.SetValue(ExifTag.DateTimeOriginal, scrapedAt.ToString("yyyy:MM:dd HH:mm:ss"));
            exifProfile.SetValue(ExifTag.OffsetTimeOriginal, "+00:00");
            exifProfile.SetValue(ExifTag.ImageDescription, $"MissionDate={targetDtoDisplay}");
            
            outputImage.SetProfile(exifProfile);
            
            string outputPath = Path.Join(outputFolder.FullName, $"{prefix}_{targetDtoDisplay}.png");
            outputImage.Write(outputPath, MagickFormat.Png);
        }
        
        PipeController.ClearTimeOverride();
    }
    
    private static DateRange PromptForRange(MissionType missionType)
    {
        // Start of range

        var startDate = PromptDate("Enter the start date (YYYY-MM-DD)");
        // Adjust LOs to start on Monday, instead.
        if (missionType == MissionType.LethalOperation)
        {
            startDate = startDate.MostRecentMonday();
            Console.WriteLine($"Date normalized to the Monday of that week ({startDate:yyyy-MM-dd})");
        }
        
        // End of range
        var endChoice = Prompt.Select<EndSpecification>("How do you want to specify the end of this scraping operation?");
        switch (endChoice)
        {
            case EndSpecification.Date:
            {
                var endDate = PromptDate("Enter the end date, inclusive (YYYY-MM-DD)");
                return new DateRange(startDate, endDate, missionType);
            }
            case EndSpecification.Span:
            {
                while (true)
                {
                    int inputSpan = Prompt.Input<int>("How many missions do you want to scrape?");
                    if (inputSpan is <= 0 or > 1000)
                    {
                        Console.Error.WriteLine($"Cannot scrape {inputSpan} missions. (1-1000)");
                        continue;
                    }
                    return new DateRange(startDate, inputSpan, missionType);
                }
            }
            default:
                // Should hypothetically never happen, but hey, you know....
                throw new InvalidOperationException("End specification must be either date or span.");
        }
    }
    
    private static DateOnly PromptDate(string promptMsg)
    {
        while (true)
        {
            string inputRaw = Prompt.Input<string>(promptMsg);
            string inputSanitized = inputRaw.Trim();
            bool inputParseResult = DateOnly.TryParseExact(
                inputSanitized,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var startDate);

            if (inputParseResult) return startDate;
            
            Console.Error.WriteLine("Could not parse date. Try again.");
        }
    }
}

internal enum EndSpecification
{
    [Display(Name = "Specify an end date, inclusive")]
    Date,
    [Display(Name = "Specify a certain amount of missions to scrape")]
    Span
}