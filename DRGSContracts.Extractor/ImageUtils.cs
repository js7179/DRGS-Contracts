using ImageMagick;

namespace DRGSContracts.Extractor;

public class ImageUtils
{
    /// <summary>
    /// Crops an image to a rectangular region defined by inclusive start and end pixel coordinates
    /// </summary>
    /// <param name="image">The source image. It is not modified by the function.</param>
    /// <param name="startX">The X-coordinate of the top-left point of the region (inclusive)</param>
    /// <param name="startY">The Y-coordinate of the top-left point of the region (inclusive)</param>
    /// <param name="endX">The X-coordinate of the bottom-right point of the region (inclusive)</param>
    /// <param name="endY">The Y-coordinate of the bottom-right point of the region (inclusive)</param>
    /// <returns>A new image containing a copy of the specified region in the original image</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startX"/> or <paramref name="startY"/> is less than zero,
    /// <paramref name="endX"/> or <paramref name="endY"/> is less than <paramref name="startX"/> or <paramref name="startY"/> respectively,
    /// <paramref name="endX"/> or <paramref name="endY"/> exceeds the image's boundaries</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="image"/> is null</exception>
    /// <remarks>Coordinates are zero-based and the origin exists at the top-left of the image.</remarks>
    public static IMagickImage<byte> CropToRegion(IMagickImage<byte> image, int startX, int startY, int endX, int endY)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentOutOfRangeException.ThrowIfLessThan(startX, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(startY, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startX, endX, nameof(endX));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startY, endY, nameof(endY));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(endX, (int)image.Width);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(endY, (int)image.Height);
        
        var geometry = new MagickGeometry(startX, startY, 
            (uint)(endX - startX + 1),
            (uint)(endY - startY + 1)); // + 1 to include last segment

        return image.CloneArea(geometry);
    }
}