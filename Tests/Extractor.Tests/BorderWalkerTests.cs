using DRGSContracts.Extractor;
using ImageMagick;
using ImageMagick.Colors;

namespace Extractor.Tests;

public class BorderWalkerTests
{
    private static readonly IMagickColor<byte> BorderColor = new ColorRGB(209, 124, 36).ToMagickColor();
    private static readonly BorderWalker Walker = new(1187, 236, BorderColor, new Percentage(5));

    [Test]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.01.png", 719)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.02.png", 719)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.03.png", 671)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.04.png", 879)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.05.png", 703)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.06.png", 703)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.07.png", 703)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.08.png", 791)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.09.png", 751)]
    [Arguments("Extractor.Tests.TestImages.BorderWalk.10.png", 863)]
    public async Task Test_FindBottomEdge__Normal(string resourceName, int expectedY)
    {
        var assembly = typeof(BorderWalkerTests).Assembly;
        await using var stream = assembly.GetManifestResourceStream(resourceName);

        await Assert.That(stream).IsNotNull().Because($"Expected embedded resource '{resourceName}' not found.");
        
        using var img = new MagickImage(stream);
        
        int actualY = Walker.FindBottomEdge(img);
        await Assert.That(actualY).IsEqualTo(expectedY);
    }

    [Test]
    public async Task Test_FindBottomEdge__OutOfBounds()
    {
        var assembly = typeof(BorderWalkerTests).Assembly;
        await using var stream = assembly.GetManifestResourceStream("Extractor.Tests.TestImages.BorderWalk.oob.png");
        
        await Assert.That(stream).IsNotNull().Because($"Expected embedded resource 'Extractor.Tests.TestImages.BorderWalk.oob.png' not found.");
        
        using var img = new MagickImage(stream); 
        
        Assert.Throws<IndexOutOfRangeException>(() => { Walker.FindBottomEdge(img); });
    }

    [Test]
    public async Task Test_FindBottomEdge_InvalidStartingColor()
    {
        var assembly = typeof(BorderWalkerTests).Assembly;
        await using var stream = assembly.GetManifestResourceStream("Extractor.Tests.TestImages.BorderWalk.invalid_start.png");
    
        await Assert.That(stream).IsNotNull().Because($"Expected embedded resource 'Extractor.Tests.TestImages.BorderWalk.invalid_start.png' not found.");

        using var img = new MagickImage(stream);
        Assert.Throws<InvalidOperationException>(() => { Walker.FindBottomEdge(img); });
    }
    
    [Test]
    public Task Test_GetColorForPixel_NullPixelColor()
    {
        var nullPixelMock = Mock.Of<IPixel<byte>>();
        nullPixelMock.ToColor().Returns((IMagickColor<byte>?)null);
        
        var pixelsMock = Mock.Of<IPixelCollection<byte>>();
        pixelsMock.GetPixel(Any<int>(), Any<int>()).Returns(nullPixelMock.Object);
        
        Assert.Throws<InvalidOperationException>(() => BorderWalker.GetColorForPixel(pixelsMock.Object, 0, 0));
        return Task.CompletedTask;
    }

    [Test]
    public Task Test_GetColorForPixel_NullPixel()
    {
        var pixelsMock = Mock.Of<IPixelCollection<byte>>();
        pixelsMock.GetPixel(Any<int>(), Any<int>()).Returns((IPixel<byte>)null);
        
        Assert.Throws<InvalidOperationException>(() => BorderWalker.GetColorForPixel(pixelsMock.Object, 0, 0));
        return Task.CompletedTask;
    }
}