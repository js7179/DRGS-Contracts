using DRGSContracts.Extractor;
using ImageMagick;

namespace Extractor.Tests.ImageUtilsTests;

public class CropToRegionTests
{
    [Test]
    [Arguments(-1, 0, 1, 1, "startX")] // startX < 0
    [Arguments(0, -1, 1, 1, "startY")] // startY < 0
    [Arguments(5, 0, 0, 5, "endX")] // startX > endX
    [Arguments(0, 5, 5, 0, "endY")] // startY > endY
    [Arguments(0, 0, 10, 1, "endX")] // endX > width
    [Arguments(0, 0, 1, 10, "endY")] // endY > height
    public async Task CropToRegion_InvalidCoordinates_Throws(int startX, int startY, int endX, int endY, string expectedParamName)
    {
        var image = new MagickImage(MagickColors.Black, 10, 10);
        var exception = await Assert.That(() => { _ =ImageUtils.CropToRegion(image, startX, startY, endX, endY); })
            .Throws<ArgumentOutOfRangeException>();
        
        await Assert.That(exception.ParamName).IsEqualTo(expectedParamName);
    }

    [Test]
    public async Task CropToRegion_NullImage_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = ImageUtils.CropToRegion(null, 0, 0, 10, 10); });
    }

    [Test]
    public async Task CropToRegion_Normal()
    {
        const int startX = 1185,
            startY = 200,
            endX = 1734,
            endY = 719;

        await using var inputStream = GetResourceStream("01_input");
        await using var expectedStream = GetResourceStream("01_expected");

        using var inputImage = new MagickImage(inputStream);
        using var clonedInputImage = inputImage.Clone();
        using var expectedImage = new MagickImage(expectedStream);
        
        var actual =  ImageUtils.CropToRegion(clonedInputImage, startX, startY, endX, endY);

        try
        {
            await Assert.That(actual.Width).IsEqualTo(expectedImage.Width);
            await Assert.That(actual.Height).IsEqualTo(expectedImage.Height);

            double distortion = actual.Compare(expectedImage, ErrorMetric.Absolute);
            await Assert.That(distortion).IsEqualTo(0)
                .Because("The result should be a pixel-exact copy of the cropped region");
            
            await Assert.That(inputImage.Compare(clonedInputImage, ErrorMetric.Absolute)).IsEqualTo(0)
                .Because("CropToRegion should not modify the input image");
        }
        catch
        {
            string path = Path.ChangeExtension(Path.GetTempFileName(), "png");
            await actual.WriteAsync(path);
            Console.WriteLine($"Crop output did not match expected. Crop output saved to: {path}");
            throw;
        }
    }
    
    private static Stream GetResourceStream(string imageName) => 
        typeof(CropToRegionTests).Assembly.GetManifestResourceStream($"Extractor.Tests.TestImages.CropToRegion.{imageName}.png")
            ?? throw new InvalidOperationException($"Extractor.Tests.TestImages.CropToRegion.{imageName}.png not found");
}