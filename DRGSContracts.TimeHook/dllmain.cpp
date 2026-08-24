#include <windows.h>
#include <MinHook.h>
#include <atomic>
#include <iostream>

#define BITMASK_ALLONE64 (uint64_t)0xFFFFFFFFFFFFFFFF

namespace {
    typedef void(WINAPI* GetSystemTimeAsFileTime_t)(LPFILETIME);

    HINSTANCE g_dll_handle;

    GetSystemTimeAsFileTime_t g_fpGetSystemTimeAsFileTime = nullptr;
    std::atomic<uint64_t> g_overrideFileTime {0 };
    const wchar_t* PIPE_NAME = L"\\\\.\\pipe\\DRGSTimeHookPipe";

    void WINAPI HookedGetSystemTimeAsFileTime(const LPFILETIME lpSystemTimeAsFileTime) {
        const uint64_t oft = g_overrideFileTime.load(std::memory_order_relaxed);
        if (oft != 0) {
            lpSystemTimeAsFileTime->dwLowDateTime = static_cast<DWORD>(oft & 0xFFFFFFFF); // lower 32-bit mask
            lpSystemTimeAsFileTime->dwHighDateTime = static_cast<DWORD>(oft >> 32);
        } else {
            g_fpGetSystemTimeAsFileTime(lpSystemTimeAsFileTime);
        }
    }

    DWORD __stdcall EjectThread(LPVOID lpParameter) {
        Sleep(100);
        FreeLibraryAndExitThread(g_dll_handle, 0);
    }

    void ShutdownTimeOverride(FILE *fp, const char* reason, MH_STATUS statusCode) {
        MH_Uninitialize();
        if (reason[0] != '\0') {
            std::cerr << reason << ": " << MH_StatusToString(statusCode) << std::endl;
        }
        Sleep(10000);
        if (fp != nullptr)
        {
            fclose(fp);
        }
        FreeConsole();
        CreateThread(0, 0, EjectThread, 0, 0, 0);
        return;
    }

    DWORD WINAPI MainLoop(LPVOID lpParameter) {
        // Set up a console so we can receive output
        AllocConsole();
        FILE *console;
        errno_t freopen_stdout_errno = freopen_s(&console, "CONOUT$", "w", stdout);
        errno_t freopen_stderr_errno = freopen_s(&console, "CONOUT$", "w", stderr);
        if (freopen_stdout_errno != 0 || freopen_stderr_errno != 0) {
            return 1;
        }
        
        // Initialize MinHook
        const MH_STATUS init_status = MH_Initialize();
        if (init_status != MH_OK) {
            ShutdownTimeOverride(console, "MH_Initialize failed", init_status);
            return 1;
        }
        
        // Resolve target pointer to `GetSystemTimeAsFileTime` hook
        const HMODULE h_kernel32 = GetModuleHandleW(L"kernel32.dll");
        const auto target_ptr = reinterpret_cast<LPVOID>(GetProcAddress(h_kernel32, "GetSystemTimeAsFileTime"));

        // Create the `GetSystemTimeAsFileTime` hook
        const MH_STATUS create_hook_api_status = MH_CreateHook(target_ptr, 
            &HookedGetSystemTimeAsFileTime,
            reinterpret_cast<LPVOID*>(&g_fpGetSystemTimeAsFileTime));
        if (create_hook_api_status != MH_OK) {
            ShutdownTimeOverride(console, "MH_CreateHook failed", create_hook_api_status);
            return 1;
        }
        
        const MH_STATUS enable_status = MH_EnableHook(target_ptr);
        if (enable_status != MH_OK) {
            ShutdownTimeOverride(console, "MH_EnableHook failed", enable_status);
            return 1;
        }
        
        std::cout << "Hook has been created and enabled" << std::endl;
        
        bool is_running = true;
        while (is_running) {
            const HANDLE h_pipe = CreateNamedPipeW(
                PIPE_NAME,
                PIPE_ACCESS_INBOUND,
                PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
                1, 0, sizeof(uint64_t), 0, nullptr);
            
            if (h_pipe == INVALID_HANDLE_VALUE) {
                std::cerr << "CreateNamedPipeW failed: " << GetLastError() << std::endl;
                Sleep(1000);
                continue;
            }
            
            // else-expression in ternary covers case when the terminal program
            // beat us to connecting to the pipe - still OK. See the final paragraph
            // in the "Return value" section of this article.
            // https://learn.microsoft.com/en-us/windows/win32/api/namedpipeapi/nf-namedpipeapi-connectnamedpipe
            const BOOL connected = ConnectNamedPipe(h_pipe, nullptr)
                ? TRUE 
                : (GetLastError() == ERROR_PIPE_CONNECTED);
            
            if (connected) {
                uint64_t nft = 0;
                DWORD bytes_read = 0;
                
                const BOOL read = ReadFile(h_pipe, &nft, sizeof(nft), &bytes_read, nullptr);
                if (read && bytes_read == sizeof(nft)) {
                    if ((nft & BITMASK_ALLONE64) == BITMASK_ALLONE64) {
                        is_running = false;
                        std::cout << "Received shutdown instruction" << std::endl;
                    } else {
                        g_overrideFileTime.store(nft, std::memory_order_relaxed);
                        if (nft == static_cast<uint64_t>(0)) {
                            std::cout << "Override temporarily disabled" << std::endl;
                        } else {
                            std::cout << "Received new file time: " << nft << std::endl;
                        }
                    }
                }
            } else {
                std::cerr << "Error connecting to pipe " << PIPE_NAME << std::endl;
            }
            
            DisconnectNamedPipe(h_pipe);
            CloseHandle(h_pipe);
        }
        
        const MH_STATUS disable_status = MH_DisableHook(target_ptr);
        if (disable_status != MH_OK) {
            ShutdownTimeOverride(console, "MH_DisableHook failed", disable_status);
            return 1;
        }
        ShutdownTimeOverride(console, "", MH_OK);
        return 0;
    }

    void UninstallHook() {
        MH_DisableHook(MH_ALL_HOOKS);
        MH_Uninitialize();
    }
};
    
BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call) {
        case DLL_PROCESS_ATTACH:
            g_dll_handle = hModule;
            CreateThread(0, 0, MainLoop, NULL, 0, NULL);
            break;
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
        case DLL_PROCESS_DETACH:
            UninstallHook();
            break;
        default:
            break;
    }
    return TRUE;
}