using ImageMagick;

namespace DRGSContracts.Extractor;

public static class BorderWalker
{
    /// <summary>
    /// From the given image, starting point, and border color & tolerance, find the bottom edge of the given mission details
    /// </summary>
    /// <param name="img">Image with mission detail screenshots</param>
    /// <param name="walkStartX">The X-coordinate where the walk should start</param>
    /// <param name="walkStartY">The Y-coordinate where the walk should start</param>
    /// <param name="walkBorderColor">The border color we should be walking</param>
    /// <param name="walkTolerance">How much of a deviation from <paramref name="walkBorderColor"/> we can be to still be considered on the border</param>
    /// <returns>The Y-coordinate of where the walk ends / the bottom corner is found</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="img"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="walkStartX"/> or <paramref name="walkStartY"/>
    /// is less than zero or greater than the width/height of the image, respectively</exception>
    /// <exception cref="InvalidOperationException">Thrown when given image does not seem to have mission details or
    ///     pixel data retrieval was unsuccessful</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when we've walked all the way through the bottom of the
    /// image and out of bounds</exception>
    public static int FindBottomEdge(IMagickImage<byte> img, int walkStartX, int walkStartY, IMagickColor<byte> walkBorderColor, Percentage walkTolerance)
    {
        ArgumentNullException.ThrowIfNull(img);
        ArgumentOutOfRangeException.ThrowIfLessThan(walkStartX, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(walkStartY, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(walkStartX, (int)img.Width);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(walkStartY, (int)img.Height);
        
        using var pixels = img.GetPixels();

        // Check starting pixel
        var startPixelColor = GetColorForPixel(pixels, walkStartX, walkStartY);
        if (!startPixelColor.FuzzyEquals(walkBorderColor, walkTolerance))
        {
            throw new InvalidOperationException($"Pixel at X={walkStartX}, Y={walkStartY} is not within tolerance of the expected border color");
        }
        
        // Actual border walking
        int imgHeight = (int)img.Height - 1; // for not walking to the very last line in the image
        for (int walkY = walkStartY; walkY < imgHeight; walkY++)
        {
            var nextPixelColor = GetColorForPixel(pixels, walkStartX, walkY + 1);
            if (!nextPixelColor.FuzzyEquals(walkBorderColor, walkTolerance))
            {
                return walkY;
            }
        }
        
        throw new IndexOutOfRangeException($"Could not find the bottom edge of the mission detail (last Y={walkStartY})");
    }

    internal static IMagickColor<byte> GetColorForPixel(IPixelCollection<byte> pixels, int x, int y)
    {
        var pixel = pixels.GetPixel(x, y) 
                    ?? throw new InvalidOperationException($"Could not retrieve pixel in border walk at X={x}, Y={y}");
        var color = pixel.ToColor() 
               ?? throw new InvalidOperationException($"Could not retrieve pixel color in border walk at X={x}, Y={y}");
        return color;
    }
}