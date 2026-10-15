using ImageMagick;

namespace DRGSContracts.Extractor;

public class BorderWalker(int walkStartX, int walkStartY, IMagickColor<byte> walkBorderColor, Percentage walkTolerance)
{
    /// <summary>
    /// From the given image, find the bottom edge of the given mission details
    /// </summary>
    /// <param name="img">Image with mission detail screenshots</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">Thrown when given image does not seem to have mission details or
    ///     pixel data retrieval was unsuccessful</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when we've walked all the way through the bottom of the
    /// image and out of bounds</exception>
    public int FindBottomEdge(IMagickImage<byte> img)
    {
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