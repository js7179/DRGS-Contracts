using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using ImageMagick;

namespace DRGSContracts.Extractor;

[ExcludeFromCodeCoverage]
internal static class ScrapeVanguard
{
    internal static Task<int> ScrapeVC(ParseResult parseResult)
    {
        var inputImageFileInfo = parseResult.GetRequiredValue<FileInfo>("input-image");
        using var inputImage = new MagickImage(inputImageFileInfo);

        using var details = Cropper.CropMissionDetails(inputImage);
        
        using var header = Cropper.CropMissionHeaderDetails(details);
        
        Program.SaveAndOpenInTemp(header);
        
        return Task.FromResult(0);
    }
}