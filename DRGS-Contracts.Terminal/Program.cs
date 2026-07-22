using System.Drawing.Imaging;
using System.Security.Principal;
using DRGS_Contracts_Terminal.Time;

namespace DRGS_Contracts_Terminal;

class Program
{
    static void Main(string[] args)
    {
        var displayCaptureSession = new DisplayCaptureSession(0, 0);
        
        Console.WriteLine($"Using GPU: {displayCaptureSession.GetGraphicsCardName()}");
        Console.WriteLine($"Using Display: {displayCaptureSession.GetDisplayDeviceName()}");
        Console.Write("Hit enter to capture the game: ");
        Console.ReadLine();
        
        var bitmapCapture = displayCaptureSession.CaptureDisplay();
        string imageLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screen.png");
        bitmapCapture.Save(imageLocation, ImageFormat.Png);
        Console.WriteLine($"Screen capture written to {imageLocation}");
    }
    
    /*static int Main(string[] args)
    {
        if (!IsRunningAsAdmin())
        {
            Console.Error.WriteLine("Program is not running as administrator - elevated privileges needed to manage system clock and W32Time service");
            return 1;
        }

        if (!WindowsTimeService.IsW32TimeServiceOff())
        {
            WindowsTimeService.DisableWindowsTimeService();
        }
        Console.WriteLine("W32Time service disabled");

        bool shouldRun = true;
        while (shouldRun)
        {
            Console.Write("DateTime (YYYY-MM-DD HH:mm:ss): ");
            var input = Console.ReadLine();
            if (input is null or "exit")
            {
                shouldRun = false;
                break;
            }

            if (DateTime.TryParse(input, out var dtUnspecified))
            {
                var dtLocal = DateTime.SpecifyKind(dtUnspecified, DateTimeKind.Local);
                SystemClockController.SetSystemClock(dtLocal);
                Console.WriteLine($"Set system clock to {dtLocal:HH:mm:ss MM-dd-yyyy}");
            }
            else
            {
                Console.Error.WriteLine("Invalid input, try again");
            }
        }
        
        Console.WriteLine("Process is exiting, re-enabling W32Time service and re-synchronizing system time");
        WindowsTimeService.EnableWindowsTimeService();
        SystemClockController.ResyncSystemTime();
        
        return 0;
    }*/
    
    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}