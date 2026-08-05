using System.Runtime.InteropServices;

namespace DRGSContracts.Terminal.GameBridge;

/// <summary>
/// Injects a DLL into a process, assuming the DLL when attached will spin off its own thread
/// </summary>
public static partial class DllInjector
{
    private const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize,
        uint flAllocationType, uint flProtect);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes,
        uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags,
        out uint lpThreadId);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);
 
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial IntPtr GetModuleHandleW(string? lpModuleName);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
 
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);
    
    /// <summary>
    /// Injects a DLL into an actively running process so the DLL can modify the functionality of the process. It starts
    /// by opening a handle to the process, allocating the filepath to the DLL within the process's memory, and loading
    /// the DLL within that process via
    /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibrarya">LoadLibraryA</see>
    /// </summary>
    /// <param name="processId">the ID of the process to inject the DLL into</param>
    /// <param name="dllPath">the filepath of the DLL to inject</param>
    /// <exception cref="InvalidOperationException">
    /// (1) If the process handle could not be opened.
    /// (2) If memory allocation for the DLL filepath could not be completed.
    /// (3) If writing the DLL filepath to process memory failed.
    /// (4) If the kernel32.dll module failed.
    /// (5) If LoadLibraryA could not be located in process memory.
    /// (6) If executing LoadLibraryA on the DLL failed. </exception>
    public static void InjectDllIntoGame(uint processId, string dllPath)
    {
        // Grab handle to the process so we can manipulate its memory
        IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            throw new InvalidOperationException($"OpenProcess failed for PID {processId}. Error: {Marshal.GetLastWin32Error()}");
        }
        
        try
        {
            // Allocate space within the process's memory to hold the DLL's filepath
            // so it can be loaded in. C-style string so we need to include the null-terminator.
            byte[] dllPathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");

            IntPtr allocatedMemoryAddress = VirtualAllocEx(hProcess,
                IntPtr.Zero, (uint)dllPathBytes.Length,
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocatedMemoryAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException($"VirtualAllocEx failed. Error {Marshal.GetLastWin32Error()}");
            }

            // Write the DLL path into process memory at the allocated space
            bool didWriteProcess = WriteProcessMemory(hProcess, allocatedMemoryAddress, dllPathBytes,
                (uint)dllPathBytes.Length, out _);
            if (!didWriteProcess)
            {
                throw new InvalidOperationException(
                    $"WriteProcessMemory failed. Error:  {Marshal.GetLastWin32Error()}");
            }

            // Resolve the memory address where the "LoadLibraryA" in kernel32.dll is
            // so we can use it to load the DLL
            IntPtr kernelHandle = GetModuleHandleW("kernel32.dll");
            if (kernelHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"GetModuleHandle failed. Error:  {Marshal.GetLastWin32Error()}");
            }
            IntPtr loadLibraryAddress = GetProcAddress(kernelHandle, "LoadLibraryA");
            if (loadLibraryAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException($"GetProcAddress failed. Error:  {Marshal.GetLastWin32Error()}");
            }

            // Start a thread that executes "LoadLibraryA", with its parameter pointing to the DLL path
            // so that the thread mounts the DLL to the process so that the DLL can modify the process
            // to hijack the GetSystemTimeAsFileTime() API call
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddress, allocatedMemoryAddress, 0,
                out _);
            if (hThread == IntPtr.Zero)
            {
                throw new InvalidOperationException($"CreateRemoteThread failed. Error:  {Marshal.GetLastWin32Error()}");
            }
            
            // Cleans up the thread and the memory space holding the DLL filepath
            WaitForSingleObject(hThread, 5000);
            CloseHandle(hThread);
            VirtualFreeEx(hProcess, allocatedMemoryAddress, (uint)dllPathBytes.Length, MEM_RELEASE);
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }
}