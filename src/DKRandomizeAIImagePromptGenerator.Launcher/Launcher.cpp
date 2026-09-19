#include <windows.h>
#include <shellapi.h>

#include <filesystem>
#include <string>
#include <vector>

namespace
{
constexpr wchar_t kAppFileName[] = L"DKRandomizeAIImagePromptGenerator.App.exe";
constexpr wchar_t kRuntimeDownloadUrl[] = L"https://dotnet.microsoft.com/download/dotnet/10.0";

std::wstring GetExecutableDirectory()
{
    std::vector<wchar_t> buffer(32768);
    const DWORD length = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (length == 0 || length >= buffer.size())
    {
        return {};
    }

    return std::filesystem::path(std::wstring(buffer.data(), length)).parent_path().wstring();
}

void AddUniqueRoot(std::vector<std::wstring>& roots, const std::wstring& root)
{
    if (root.empty())
    {
        return;
    }

    std::error_code error;
    auto normalized = std::filesystem::weakly_canonical(root, error).wstring();
    if (error)
    {
        normalized = root;
    }

    for (const auto& existing : roots)
    {
        if (_wcsicmp(existing.c_str(), normalized.c_str()) == 0)
        {
            return;
        }
    }

    roots.push_back(normalized);
}

void AddEnvironmentRoot(std::vector<std::wstring>& roots, const wchar_t* variableName)
{
    const DWORD required = GetEnvironmentVariableW(variableName, nullptr, 0);
    if (required <= 1)
    {
        return;
    }

    std::vector<wchar_t> value(required);
    if (GetEnvironmentVariableW(variableName, value.data(), required) > 0)
    {
        AddUniqueRoot(roots, value.data());
    }
}

void AddRegistryRoot(std::vector<std::wstring>& roots)
{
    HKEY key = nullptr;
    if (RegOpenKeyExW(
            HKEY_LOCAL_MACHINE,
            L"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x64",
            0,
            KEY_READ | KEY_WOW64_64KEY,
            &key) != ERROR_SUCCESS)
    {
        return;
    }

    DWORD type = 0;
    DWORD bytes = 0;
    if (RegQueryValueExW(key, L"InstallLocation", nullptr, &type, nullptr, &bytes) == ERROR_SUCCESS &&
        (type == REG_SZ || type == REG_EXPAND_SZ) &&
        bytes >= sizeof(wchar_t))
    {
        std::vector<wchar_t> value((bytes / sizeof(wchar_t)) + 1, L'\0');
        if (RegQueryValueExW(
                key,
                L"InstallLocation",
                nullptr,
                &type,
                reinterpret_cast<LPBYTE>(value.data()),
                &bytes) == ERROR_SUCCESS)
        {
            if (type == REG_EXPAND_SZ)
            {
                const DWORD expandedRequired = ExpandEnvironmentStringsW(value.data(), nullptr, 0);
                if (expandedRequired > 1)
                {
                    std::vector<wchar_t> expanded(expandedRequired);
                    if (ExpandEnvironmentStringsW(
                            value.data(),
                            expanded.data(),
                            expandedRequired) > 0)
                    {
                        AddUniqueRoot(roots, expanded.data());
                    }
                }
            }
            else
            {
                AddUniqueRoot(roots, value.data());
            }
        }
    }

    RegCloseKey(key);
}

bool ContainsDesktopRuntime10(const std::wstring& dotnetRoot)
{
    const auto sharedFx = std::filesystem::path(dotnetRoot)
        / L"shared"
        / L"Microsoft.WindowsDesktop.App";

    std::error_code error;
    if (!std::filesystem::is_directory(sharedFx, error))
    {
        return false;
    }

    for (const auto& entry : std::filesystem::directory_iterator(sharedFx, error))
    {
        if (error)
        {
            return false;
        }

        if (!entry.is_directory(error))
        {
            continue;
        }

        const auto version = entry.path().filename().wstring();
        int major = -1;
        int minor = -1;
        if (swscanf_s(version.c_str(), L"%d.%d", &major, &minor) == 2 &&
            major == 10)
        {
            return true;
        }
    }

    return false;
}

bool HasDesktopRuntime10()
{
    std::vector<std::wstring> roots;
    AddEnvironmentRoot(roots, L"DOTNET_ROOT_X64");
    AddEnvironmentRoot(roots, L"DOTNET_ROOT");
    AddRegistryRoot(roots);

    wchar_t programFiles[MAX_PATH] = {};
    if (GetEnvironmentVariableW(
            L"ProgramW6432",
            programFiles,
            static_cast<DWORD>(std::size(programFiles))) > 0)
    {
        AddUniqueRoot(roots, (std::filesystem::path(programFiles) / L"dotnet").wstring());
    }

    for (const auto& root : roots)
    {
        if (ContainsDesktopRuntime10(root))
        {
            return true;
        }
    }

    return false;
}

bool HasArgument(const wchar_t* expected)
{
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (argv == nullptr)
    {
        return false;
    }

    bool found = false;
    for (int index = 1; index < argc; ++index)
    {
        if (_wcsicmp(argv[index], expected) == 0)
        {
            found = true;
            break;
        }
    }

    LocalFree(argv);
    return found;
}

bool StartApplication(const std::wstring& appPath, const std::wstring& workingDirectory)
{
    std::wstring commandLine = L"\"" + appPath + L"\"";

    STARTUPINFOW startupInfo{};
    startupInfo.cb = sizeof(startupInfo);
    PROCESS_INFORMATION processInfo{};

    const BOOL started = CreateProcessW(
        appPath.c_str(),
        commandLine.data(),
        nullptr,
        nullptr,
        FALSE,
        0,
        nullptr,
        workingDirectory.c_str(),
        &startupInfo,
        &processInfo);

    if (!started)
    {
        return false;
    }

    CloseHandle(processInfo.hThread);
    CloseHandle(processInfo.hProcess);
    return true;
}

void ShowMissingRuntimeMessage()
{
    const int choice = MessageBoxW(
        nullptr,
        L"이 프로그램을 실행하려면 Microsoft .NET 10 Desktop Runtime (x64)이 필요합니다.\n\n"
        L"Microsoft 공식 다운로드 페이지를 여시겠습니까?",
        L"DK Randomize AI Image Prompt Generator",
        MB_YESNO | MB_ICONINFORMATION | MB_SETFOREGROUND);

    if (choice == IDYES)
    {
        ShellExecuteW(
            nullptr,
            L"open",
            kRuntimeDownloadUrl,
            nullptr,
            nullptr,
            SW_SHOWNORMAL);
    }
}
}

int WINAPI wWinMain(
    HINSTANCE,
    HINSTANCE,
    PWSTR,
    int)
{
    if (HasArgument(L"--runtime-check"))
    {
        return HasDesktopRuntime10() ? 0 : 10;
    }

    if (!HasDesktopRuntime10())
    {
        ShowMissingRuntimeMessage();
        return 10;
    }

    const auto directory = GetExecutableDirectory();
    if (directory.empty())
    {
        MessageBoxW(
            nullptr,
            L"프로그램 실행 폴더를 확인할 수 없습니다.",
            L"DK Randomize AI Image Prompt Generator",
            MB_OK | MB_ICONERROR);
        return 20;
    }

    const auto appPath = (std::filesystem::path(directory) / kAppFileName).wstring();
    if (!std::filesystem::is_regular_file(appPath))
    {
        MessageBoxW(
            nullptr,
            L"실제 프로그램 파일(DKRandomizeAIImagePromptGenerator.App.exe)을 찾을 수 없습니다.\n"
            L"배포 ZIP을 다시 압축 해제해 주세요.",
            L"DK Randomize AI Image Prompt Generator",
            MB_OK | MB_ICONERROR);
        return 21;
    }

    if (!StartApplication(appPath, directory))
    {
        MessageBoxW(
            nullptr,
            L"프로그램을 시작하지 못했습니다.",
            L"DK Randomize AI Image Prompt Generator",
            MB_OK | MB_ICONERROR);
        return 22;
    }

    return 0;
}
