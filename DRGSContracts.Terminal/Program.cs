using System.Collections.Immutable;
using System.CommandLine;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using DRGSContracts.Terminal.DisplayCapture;
using DRGSContracts.Terminal.GameBridge;
using ScreenCapture.NET;
using Sharprompt;

namespace DRGSContracts.Terminal;

internal static class Program
{
    private static readonly string GAME_EXENAME = "DRG Survivor";
    private static readonly string DLL_PATH = Path.Combine(AppContext.BaseDirectory, "TimeHook.dll");
    
    private static int Main(string[] args)
    {
        // Parse the command-line arguments
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
        
        // Find the game process and inject
        uint pid = GetGameProcessId();
        DllInjector.InjectDllIntoGame(pid, DLL_PATH);
        Console.WriteLine($"Injected DLL into game with PID {pid}");
        
        while (true)
        {
            Console.Write("Enter a date to override (YYYY-MM-DD): ");
            string? input = Console.ReadLine();
            if (input is null or "exit")
            {
                PipeController.ShutdownOverride();
                break;
            }

            if (input is "" or "0")
            {
                PipeController.ClearTimeOverride();
                continue;
            }

            string inputTrimmed = input.Trim();
            
            bool tryParse = DateOnly.TryParseExact(inputTrimmed, "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly);
            if (!tryParse)
            {
                Console.Error.WriteLine("Could not parse date. Try again.");
                continue;
            }

            var dto = new DateTimeOffset(dateOnly, new TimeOnly(6, 5), TimeSpan.Zero);
            PipeController.SendDate(dto);
            Console.WriteLine($"Wrote {dto:yyyy-MM-dd} to the pipe (filetime {dto.ToFileTime()})");
        }
        
        /*
        // Main loop of the program
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
        }*/
        
        return 0;
    }

    /// <summary>
    /// Helper function that searches for the game's process, prompting the user until it finds the game process
    /// and returns its associated PID. If there are multiple copies of the game running, then the first one is taken
    /// and returned. 
    /// </summary>
    /// <returns>The process ID of the game</returns>
    private static uint GetGameProcessId()
    {
        var processes = Process.GetProcessesByName(GAME_EXENAME);
        while (processes.Length == 0)
        {
            Console.WriteLine($"Could not find \"{GAME_EXENAME}\" to inject into. Hit enter when the game has been started: ");
            Console.ReadLine();
            processes = Process.GetProcessesByName(GAME_EXENAME);
        }
        var gameProcess = processes.First();
        uint pid = checked((uint)gameProcess.Id);
        return pid;
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

