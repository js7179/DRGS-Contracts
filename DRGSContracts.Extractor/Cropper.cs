using ImageMagick;
using ImageMagick.Colors;

namespace DRGSContracts.Extractor;

public static class Cropper
{
    private static readonly IMagickColor<byte> BorderColor = new ColorRGB(209, 124, 36).ToMagickColor();
    private static readonly Percentage BorderWalkTolerance = new(5);
    
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

    /// <summary>
    /// Ingests a screenshot of the entire game and crops it to just the mission detail
    /// for further processing
    /// </summary>
    /// <param name="image">A screengrab of the game with the mission details somewhere on it</param>
    /// <returns>A deep copy of the mission details</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null</exception>
    public static IMagickImage<byte> CropMissionDetails(IMagickImage<byte> image)
    {
        ArgumentNullException.ThrowIfNull(image);
        
        int bottomBorderY =
            BorderWalker.FindBottomEdge(image, 1187, 236, BorderColor, BorderWalkTolerance);
        Logger.Instance.LogDebug($"Found the bottom border of mission details at Y={bottomBorderY}");
        
        return CropToRegion(image, 1185, 200, 1734, bottomBorderY);
    }
    
    /// <summary>
    /// Crops a screenshot of mission details to just the header where the contract identifier lives
    /// </summary>
    /// <param name="image">The source image. This is cropped to the mission detail.</param>
    /// <returns>A deep copy of just the header for the mission details</returns>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null</exception>
    public static IMagickImage<byte> CropMissionHeaderDetails(IMagickImage<byte> image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return CropToRegion(image, 0, 0, (int)image.Width - 1, 35);
    }
}