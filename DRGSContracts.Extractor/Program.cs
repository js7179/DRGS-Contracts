using System.CommandLine;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ImageMagick;

namespace DRGSContracts.Extractor;

[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args)
    {
        RootCommand rootCommand = new("Extracts mission details out of a Deep Rock Galactic: Survivor screengrab");
        
        Option<bool> debugOption = new("--debug", "-d")
        {
            Description = "Enable debug output",
            DefaultValueFactory = _ => false,
        };
        rootCommand.Options.Add(debugOption);

        Argument<FileInfo> inputImageArg = new("input-image")
        {
            Description = "The screenshot of the specific mission detail to scrape",
            Arity = ArgumentArity.ExactlyOne,
        };
        inputImageArg.AcceptExistingOnly();

        Argument<FileInfo> outputJsonArg = new("output-json")
        {
            Description = "Where the resulting JSON will be written to",
            Arity = ArgumentArity.ExactlyOne
        };
        outputJsonArg.AcceptLegalFilePathsOnly();

        Command vcCommand = new("vanguard", "Extracts Vanguard Contract details") { inputImageArg, outputJsonArg };
        Command loCommand = new("lethal", "Extracts Lethal Operation details") { inputImageArg, outputJsonArg };
        vcCommand.Aliases.Add("vc");
        loCommand.Aliases.Add("lo");
        vcCommand.SetAction(ScrapeVanguard.ScrapeVC);
        loCommand.SetAction(ScrapeLethal.ScrapeLO);
        
        rootCommand.Add(vcCommand);
        rootCommand.Add(loCommand);

        var parseResult = rootCommand.Parse(args);
        Logger.Init(parseResult.GetValue(debugOption));
        
        return parseResult.Invoke();
    }

    /// <summary>
    /// Temporary helper method that saves an image to disk in the temp folder
    /// and opens it in the default image viewer.
    /// Remove when done implementing Extractor.
    /// </summary>
    /// <param name="image">Image to save and open</param>
    internal static void SaveAndOpenInTemp(IMagickImage<byte> image)
    {
        string imgFilename = Path.ChangeExtension(Path.GetTempFileName(), "png");
        
        image.Write(imgFilename);
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = imgFilename,
            UseShellExecute = true
        });
        Logger.Instance.LogInfo($"Image saved to {imgFilename}, opening in default image viewer");
    }
}