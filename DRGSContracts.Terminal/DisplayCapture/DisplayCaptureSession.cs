using System.Collections.Immutable;
using System.Drawing;
using HPPH;
using HPPH.System.Drawing;
using ScreenCapture.NET;

namespace DRGSContracts.Terminal.DisplayCapture;

/// <summary>
/// A <see href="https://en.wikipedia.org/wiki/Facade_pattern">Facade</see> class that simplifies capturing
/// a specific display using the <see href="https://github.com/DarthAffe/ScreenCapture.NET">ScreenCapture.NET</see>
/// library and its DX11-based extension
/// </summary>
public sealed class DisplayCaptureSession : IDisposable
{
    private readonly DX11ScreenCaptureService _screenCaptureService;
    private readonly GraphicsCard _gpu;
    private readonly Display _display;
    private readonly DX11ScreenCapture _screenCapture;

    private readonly CaptureZone<ColorBGRA> _fullscreenZone;

    private bool _isDisposed = false;
    
    /// <summary>
    /// Instantiates the facade to provide an easy-to-use method to capture
    /// a given display 
    /// </summary>
    /// <param name="gpuIndex">The index of the GPU to capture the display from, 0-based</param>
    /// <param name="displayIndex">The index of the display of the given GPU to capture from, 0-based</param>
    /// <exception cref="ArgumentOutOfRangeException">GPU Index or Display Index for given GPU is out of bounds</exception>
    public DisplayCaptureSession(int gpuIndex, int displayIndex)
    {
        _screenCaptureService = new DX11ScreenCaptureService();
        var gpusAvailable = _screenCaptureService.GetGraphicsCards().ToImmutableList();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(gpuIndex, gpusAvailable.Count, nameof(gpuIndex));
        _gpu = gpusAvailable[gpuIndex];
        var displays = _screenCaptureService.GetDisplays(_gpu).ToImmutableList();
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(displayIndex, displays.Count, nameof(displayIndex));
        _display = displays[displayIndex];
        _screenCapture = _screenCaptureService.GetScreenCapture(_display);
        _fullscreenZone = _screenCapture.RegisterCaptureZone(0, 0, _display.Width, _display.Height, 0);
    }

    /// <summary>
    /// Captures what is on the display associated with this session
    /// </summary>
    /// <returns>A <see cref="System.Drawing.Bitmap"/> of the current screen, which can be saved or manipulated</returns>
    public Bitmap CaptureDisplay()
    {
        _screenCapture.CaptureScreen();

        using (_fullscreenZone.Lock())
        {
            IImage image = _fullscreenZone.Image;
            var bitmap = image.ToBitmap();
            return bitmap;
        }
    }

    /// <summary>
    /// Returns the name of the graphics card we are capturing a display from
    /// </summary>
    /// <returns>The name of the graphics card we are capturing a display from</returns>
    public string GetGraphicsCardName()
    {
        return _gpu.Name;
    }

    /// <summary>
    /// Returns the internal device name of the display we are capturing
    /// </summary>
    /// <returns>The internal device name of the display we are capturing</returns>
    public string GetDisplayDeviceName()
    {
        return _display.DeviceName;
    }

    /// <summary>
    /// Releases the <see cref="ScreenCapture.NET.DX11ScreenCapture"/> and <see cref="ScreenCapture.NET.DX11ScreenCaptureService"/> managed resource 
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _screenCapture.Dispose();
        _screenCaptureService.Dispose();
        _isDisposed = true;
    }
}