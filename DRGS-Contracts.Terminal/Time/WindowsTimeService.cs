using System.ServiceProcess;

namespace DRGS_Contracts_Terminal.Time;

/// <summary>
/// This class is responsible for managing the <see href="https://learn.microsoft.com/en-us/windows-server/networking/windows-time-service/windows-time-service-top">W32Time</see> service on Windows platform,
/// which manages time synchronization for the current device. Users can disable, re-enable, and poll the status of the service.
/// It uses the <see cref="System.ServiceProcess.ServiceController">ServiceController</see> API to monitor the service.
/// </summary>
public static class WindowsTimeService
{
    private static readonly ServiceController W32TimeService = new("W32Time");
    private static readonly TimeSpan ServiceTransitionTimeout = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Stops the W32Time service, returning when it is fully stopped
    /// </summary>
    /// <exception cref="System.ServiceProcess.TimeoutException">Thrown if the service is not stopped within the timeout period</exception>
    public static void DisableWindowsTimeService()
    {
        W32TimeService.Stop();
        W32TimeService.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTransitionTimeout);
    }

    /// <summary>
    /// Starts the W32Time service, returning when it has started again
    /// </summary>
    /// <exception cref="System.ServiceProcess.TimeoutException">Thrown if the service is not running within the timeout period</exception>
    public static void EnableWindowsTimeService()
    {
        W32TimeService.Start();
        W32TimeService.WaitForStatus(ServiceControllerStatus.Running, ServiceTransitionTimeout);
    }

    /// <summary>
    /// Checks if the W32Time service is stopped
    /// </summary>
    /// <returns>If the W32Time service is stopped</returns>
    public static bool IsW32TimeServiceOff()
    {
        return W32TimeService.Status is ServiceControllerStatus.Stopped;
    }
    
}