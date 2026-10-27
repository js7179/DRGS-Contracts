using System.Diagnostics.CodeAnalysis;

namespace DRGSContracts.Extractor;

[ExcludeFromCodeCoverage]
internal class Logger
{
    private static Logger? _instance;
    private readonly bool _debug = false;
    
    internal static Logger Instance => _instance
        ?? throw new InvalidOperationException("Logger has not been initialized");

    internal static void Init(bool debugMode)
    {
        if(_instance is not null)
            throw new InvalidOperationException("Logger is already initialized");
        _instance = new Logger(debugMode);
    }
    
    private Logger(bool debugMode)
    {
        _debug = debugMode;
    }

    public void LogInfo(string msg) => Log("INFO", msg);
    
    public void LogDebug(string msg)
    {
        if (!_debug) return;
        Log("DEBUG", msg);
    }
    
    private static void Log(string level, string msg) => Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {msg}");
}