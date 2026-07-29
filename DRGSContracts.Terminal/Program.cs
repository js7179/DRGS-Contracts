using System.Collections.Immutable;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Security.Principal;
using DRGSContracts.Terminal.Time;
using DRGSContracts.Terminal.DisplayCapture;
using ScreenCapture.NET;
using Sharprompt;

namespace DRGSContracts.Terminal;

class Program
{
    private static int Main(string[] args)
    {
        /*if (!IsRunningAsAdmin())
        {
            Console.Error.WriteLine("Program is not running as administrator - elevated privileges needed to manage system clock and W32Time service");
            return 1;
        }*/

        RootCommand rootCommand = new("Program that screengrabs VC/LO information");
        var opts = CliOptions.GetProgramOptions();
        foreach(var opt in opts)
            rootCommand.Options.Add(opt);
        
        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count > 0)
        {
            foreach (var error in parseResult.Errors)
                Console.Error.WriteLine(error.Message);
            return 1;
        }

        int gpuIndex = parseResult.GetRequiredValue<int>("--gpu-index"),
            displayIndex = parseResult.GetRequiredValue<int>("--display-index");
        var outputFolder = parseResult.GetRequiredValue<DirectoryInfo>("--output-folder");
        
        bool shouldExit = false;

        while (!shouldExit)
        {
            var value = Prompt.Select<Action>("Choose an action:");
            switch (value)
            {
                case Action.ScrapeVanguard:
                    Console.Error.WriteLine("Not implemented yet");
                    break;
                case Action.ScrapeLethal:
                    Console.Error.WriteLine("Not implemented yet");
                    break;
                case Action.ConfigureDisplay:
                    var newIndices = ConfigureDisplay(gpuIndex, displayIndex);
                    gpuIndex = newIndices.gpuIndex;
                    displayIndex = newIndices.displayIndex;
                    Console.WriteLine($"Switched capture target to display index {displayIndex}");
                    break;
                case Action.TestScreenshot:
                    string imgFilename = TestScreenshot(gpuIndex, displayIndex);
                    Console.WriteLine($"Screenshot saved to {imgFilename} and opened in your default viewer");
                    Console.WriteLine("If you do not see it, re-configure it now or restart the program with a different --gpu-index and --display-index");
                    break;
                case Action.Exit:
                    shouldExit = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown action: {value}");
                    continue;
            }
        }
        
        return 0;
    }
    
    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Tests capturing the game by saving a screen capture to a temp
    /// image and opens it up in the user's default image viewer
    /// </summary>
    /// <param name="gpuIndex">The index of the GPU we are capturing output from</param>
    /// <param name="displayIndex">The index of the display we are capturing</param>
    /// <returns>The path to the image file, usually located in the temp folder</returns>
    private static string TestScreenshot(int gpuIndex, int displayIndex)
    {
        using var captureSession = new DisplayCaptureSession(gpuIndex, displayIndex);
        string imgFilename = Path.ChangeExtension(Path.GetTempFileName(), "png");
        var capture = captureSession.CaptureDisplay();
        capture.Save(imgFilename, ImageFormat.Png);
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = imgFilename,
            UseShellExecute = true
        });
        return imgFilename;
    }

    /// <summary>
    /// Allows the user to switch what display they are capturing
    /// </summary>
    /// <param name="currGpuIndex">What GPU did the user have selected before</param>
    /// <param name="currDisplayIndex">What display did the user have selected before</param>
    /// <returns></returns>
    private static (int gpuIndex, int displayIndex) ConfigureDisplay(int currGpuIndex, int currDisplayIndex)
    {
        using var svc = new DX11ScreenCaptureService();
        
        // Prompt for GPU
        var gpuArray = svc.GetGraphicsCards().ToImmutableArray();
        var gpuDisplayNames = gpuArray.Select(gpu => gpu.Name).ToImmutableArray();
        string newGpuName = Prompt.Select("Select which graphics card to capture:", gpuDisplayNames, defaultValue: gpuDisplayNames[currGpuIndex]);
        int newGpuIndex = gpuDisplayNames.IndexOf(newGpuName);
        
        var displayArray = svc.GetDisplays(gpuArray[newGpuIndex]).ToImmutableArray();
        var displayNames = displayArray.Select(display => display.DeviceName).ToImmutableArray();
        string defaultValueForDisplayPrompt = (newGpuIndex == currGpuIndex) ? displayNames[currDisplayIndex] : displayNames[0];
        string newDisplayName = Prompt.Select("Select which display to capture:", displayNames, defaultValue: defaultValueForDisplayPrompt);
        int newDisplayIndex = displayNames.IndexOf(newDisplayName);
        
        return (newGpuIndex, newDisplayIndex);
    }
    
    /// <summary>
    /// Enum type for the main program's prompt loop
    /// See <see href="https://github.com/shibayan/Sharprompt/blob/master/README.md#enum-type-support">Sharprompt's Enum type support</see> for more information.
    /// </summary>
    internal enum Action
    {
        [Display(Name = "Scrape Vanguard Contracts")]
        ScrapeVanguard,
    
        [Display(Name = "Scrape Lethal Operations")]
        ScrapeLethal,
    
        [Display(Name = "Configure what display to capture")]
        ConfigureDisplay,
 
        [Display(Name = "Take a test screenshot")]
        TestScreenshot,
    
        [Display(Name = "Exit the program")]
        Exit
    }
}

