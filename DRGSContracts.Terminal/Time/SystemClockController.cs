using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DRGSContracts.Terminal.Time;

/// <summary>
/// This class is responsible for controlling the system clock. Users can set a specified date & time,
/// or indicate to the system that it should re-synchronize the system clock to the timeservers.
/// </summary>
public static class SystemClockController
{
    private static readonly TimeSpan ResyncTimeout = TimeSpan.FromSeconds(30);
    
    // ushort matches the 16-bit WORD type used by the Windows API
    /// <summary>
    /// Exposes the <see href="https://learn.microsoft.com/en-us/windows/win32/api/minwinbase/ns-minwinbase-systemtime">SYSTEMTIME</see> structure from the Windows API
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEMTIME
    {
        public ushort wYear, wMonth, wDayOfWeek, wDay, wHour, wMinute, wSecond, wMillisecond;
    }
    
    /// <summary>
    /// Exposes the <see href="https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-setsystemtime">SetSystemTime</see> method from the Windows API 
    /// </summary>
    /// <param name="st">Systemtime struct to set system clock to</param>
    /// <returns>True if the operation was a success. If it fails, then check GetLastError. For more information, see the hyperlink above.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetSystemTime(ref SYSTEMTIME st);

    /// <summary>
    /// Exposes the <see href="https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtime">GetSystemTime</see> method from the Windows API
    /// </summary>
    /// <param name="st">Systemtime struct that the current system clock is written to</param>
    [DllImport("kernel32.dll")]
    private static extern void GetSystemTime(out SYSTEMTIME st);
    
    /// <summary>
    /// Instructs the W32Time service to re-synchronize the system clock. It has a timeout window, and if it is exceeded, will attempt
    /// to kill the re-synchronization.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the w32tm.exe process could not be started or if the W32Time service isn't running</exception>
    /// <exception cref="TimeoutException">If w32tm.exe exceeds the timeout window for re-synchronization</exception>
    /// <exception cref="ExternalException">If w32tm.exe returns with a failure code</exception> 
    public static void ResyncSystemTime()
    {
        if (WindowsTimeService.IsW32TimeServiceOff())
        {
            throw new InvalidOperationException("W32TimeService is off - call EnableWindowsTimeService() first");
        }
        var processInfo = new ProcessStartInfo("w32tm.exe", "/resync")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Verb = "runas",
        };
        
        using var process = Process.Start(processInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start w32tm.exe process.");
        }

        if (!process.WaitForExit(ResyncTimeout))
        {
            process.Kill();
            throw new TimeoutException($"w32tm.exe did not exit within {ResyncTimeout.TotalSeconds} seconds");
        }

        if (process.ExitCode != 0)
        {
            throw new ExternalException($"w32tm.exe /resync exited with code {process.ExitCode}");
        }
    }

    /// <summary>
    /// Sets the system clock to the specified <see cref="DateTime">DateTime</see>
    /// </summary>
    /// <param name="dateTime">The date and time to set the system clock to.
    ///     The kind must be Utc or Local.
    ///     If kind is Unspecified, an ArgumentException is thrown.
    ///     If kind is local, it will automatically be converted to UTC.
    ///     </param>
    /// <exception cref="InvalidOperationException">Thrown if the W32Time service is still running</exception>
    /// <exception cref="Win32Exception">Thrown when setting the system clock fails</exception>
    /// <exception cref="ArgumentException">Thrown when DateTime parameter is unspecified kind</exception>
    public static void SetSystemClock(DateTime dateTime)
    {
        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            throw new ArgumentException("dateTime.Kind must not be Unspecified - specify Utc or Local explicitly", nameof(dateTime));
        }
        if (!WindowsTimeService.IsW32TimeServiceOff())
        {
            throw new InvalidOperationException("W32TimeService is on - call DisableWindowsTimeService() first");
        }

        var utcDT = dateTime.ToUniversalTime();

        var st = new SYSTEMTIME
        {
            wYear = (ushort)utcDT.Year,
            wMonth = (ushort)utcDT.Month,
            wDay = (ushort)utcDT.Day,
            wHour = (ushort)utcDT.Hour,
            wMinute = (ushort)utcDT.Minute,
            wSecond = (ushort)utcDT.Second,
            wMillisecond = (ushort)utcDT.Millisecond,
        };

        if (!SetSystemTime(ref st))
        {
            int err = Marshal.GetLastWin32Error();
            throw new Win32Exception(err);
        }
    }

    /// <summary>
    /// Retrieves the system clock as it is - even if it's overridden
    /// </summary>
    /// <returns>DateTime struct of the current system clock</returns>
    public static DateTime GetSystemClock()
    {
        GetSystemTime(out SYSTEMTIME st);

        return new DateTime(
            st.wYear, st.wMonth, st.wDay,
            st.wHour, st.wMinute, st.wSecond, st.wMillisecond, 
            DateTimeKind.Utc);
    }
}