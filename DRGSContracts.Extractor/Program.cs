using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ImageMagick;
using ImageMagick.Colors;

namespace DRGSContracts.Extractor;

[ExcludeFromCodeCoverage]
class Program
{
    static int Main(string[] args)
    {
        /*if (args.Length != 2)
        {
            Console.Error.WriteLine("Expected two arguments: input.png output.json");
            return;
        }*/
        
        var imgInputFile = new FileInfo(args[0]);
        // var jsonOutputFile = new FileInfo(args[1]);

        using var imageInput = new MagickImage(imgInputFile);

        if (imageInput.BaseWidth != 1920 || imageInput.BaseHeight != 1080)
        {
            Console.Error.WriteLine("Program does not support any resolution other than 1920 by 1080.");
            return 1;
        }

        var borderColor = new ColorRGB(209, 124, 36).ToMagickColor();
        
        var borderWalker = new BorderWalker(1187, 236, borderColor, new Percentage(5));
        int borderBottomY = borderWalker.FindBottomEdge(imageInput);
        
        Console.WriteLine($"Bottom border for this image is y={borderBottomY}");
        
        using var cropped = ImageUtils.CropToRegion(imageInput, 1185, 200, 1734, borderBottomY);
        
        string imgFilename = Path.ChangeExtension(Path.GetTempFileName(), "png");
        
        cropped.Write(imgFilename);
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = imgFilename,
            UseShellExecute = true
        });
        Console.WriteLine($"Crop output posted to {imgFilename}");
        return 0;
    }
}