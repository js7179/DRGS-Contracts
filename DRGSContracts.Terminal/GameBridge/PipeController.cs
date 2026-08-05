using System.IO.Pipes;

namespace DRGSContracts.Terminal.GameBridge;

/// <summary>
/// Handles communication over the named injection pipe
/// </summary>
public static class PipeController
{
    /// <summary>
    /// The short name of the pipe to connect to send a payload to
    /// </summary>
    private const string PipeName = "DRGSTimeHookPipe";
    private const ulong ShutdownCommand = 0xFFFFFFFFFFFFFFFF;
    private const ulong ResetCommand = 0x0;
    private const int PipeConnectionTimeoutMs = 10 * 1000;

    /// <summary>
    /// Creates the pipe stream, connects to it, and sends a 64-bit unsigned integer payload
    /// to the pipe to override system time for the game
    /// </summary>
    /// <param name="nft">The payload to send over the pipe</param>
    /// <remarks>Sending a 0x0 payload resets system time override and restores normal functionality while
    /// sending a payload with all bits flipped to 1 shuts down the time override hook</remarks>
    private static void SetTimeOverride(ulong nft)
    {
        using var pipeClientStream = new NamedPipeClientStream(".",  PipeName, PipeDirection.Out, PipeOptions.None);
        using var binaryWriter = new BinaryWriter(pipeClientStream);
        try
        {
            pipeClientStream.Connect(PipeConnectionTimeoutMs);
            binaryWriter.Write(nft);
            binaryWriter.Flush();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not send information across the pipe: {ex.Message}");
        }
        // No need to close streams - both BinaryWriter and NamedPipeClientStream utilize IDisposable
        // so "using" will automatically close them when we go out-of-scope
    }

    /// <summary>
    /// Instructs the hook to reset - basically restoring normal functionality to system time retrieval
    /// </summary>
    public static void ClearTimeOverride()
    {
        SetTimeOverride(ResetCommand);
    }

    /// <summary>
    /// Instructs the hook to shut down and unhook from the game
    /// </summary>
    public static void ShutdownOverride()
    {
        SetTimeOverride(ShutdownCommand);
    }

    /// <summary>
    /// Instructs the hook to set what system time the process sees to a specific
    /// point in time
    /// </summary>
    /// <param name="dto">The date and time to set as the override</param>
    public static void SendDate(DateTimeOffset dto)
    {
        ulong nft = checked((ulong)dto.ToFileTime());
        SetTimeOverride(nft);
    }
    
}