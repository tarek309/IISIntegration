// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

HOSTFXR_UTILITY::HOSTFXR_UTILITY()
{
}

HOSTFXR_UTILITY::~HOSTFXR_UTILITY()
{
}

//
// Runs a standalone appliction.
// The folder structure looks like this:
// Application/
//   hostfxr.dll
//   Application.exe
//   Application.dll
//   etc.
// We get the full path to hostfxr.dll and Application.dll and run hostfxr_main,
// passing in Application.dll.
// Assuming we don't need Application.exe as the dll is the actual application.
//
HRESULT
HOSTFXR_UTILITY::GetStandaloneHostfxrParameters(
    PCWSTR              pwzExePath,
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT             hr = S_OK;
    STRU                struDllPath;
    STRU                struArguments;
    DWORD               dwPosition;

    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struDllPath.Copy(pwzExePath)))
    {
        goto Finished;
    }

    dwPosition = struDllPath.LastIndexOf(L'.', 0);
    if (dwPosition == -1)
    {
        hr = E_FAIL;
        goto Finished;
    }

    struDllPath.QueryStr()[dwPosition] = L'\0';

    if (FAILED(hr = struDllPath.SyncWithBuffer()) ||
        FAILED(hr = struDllPath.Append(L".dll")))
    {
        goto Finished;
    }

    if (!UTILITY::CheckIfFileExists(struDllPath.QueryStr()))
    {
        // Treat access issue as File not found
        hr = ERROR_FILE_NOT_FOUND;
        goto Finished;
    }

    if (FAILED(hr = struArguments.Copy(struDllPath)) ||
        FAILED(hr = struArguments.Append(L" ")) ||
        FAILED(hr = struArguments.Append(pConfig->QueryArguments())))
    {
        goto Finished;
    }

    if (FAILED(hr = SetHostFxrArguments(struArguments.QueryStr(), pwzExePath, pConfig)))
    {
        goto Finished;
    }

Finished:

    return hr;
}

HRESULT
HOSTFXR_UTILITY::GetHostFxrParameters(
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT                     hr = S_OK;
    STRU                        struSystemPathVariable;
    STRU                        struHostFxrPath;
    STRU                        struExeLocation;
    STRU                        struHostFxrSearchExpression;
    STRU                        struHighestDotnetVersion;
    std::vector<std::wstring>   vVersionFolders;
    DWORD                       dwPosition;

    // Convert the process path an absolute path.
    hr = UTILITY::ConvertPathToFullPath(
        pConfig->QueryProcessPath()->QueryStr(),
        pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        &struExeLocation
    );

    if (FAILED(hr))
    {
        goto Finished;
    }

    if (UTILITY::CheckIfFileExists(struExeLocation.QueryStr()))
    {
        // Check if hostfxr is in this folder, if it is, we are a standalone application,
        // else we assume we received an absolute path to dotnet.exe
        hr = UTILITY::ConvertPathToFullPath(L".\\hostfxr.dll", pConfig->QueryApplicationPhysicalPath()->QueryStr(), &struHostFxrPath);
        if (FAILED(hr))
        {
            goto Finished;
        }

        if (UTILITY::CheckIfFileExists(struHostFxrPath.QueryStr()))
        {
            // Standalone application
            if (FAILED(hr = pConfig->SetHostFxrFullPath(struHostFxrPath.QueryStr())))
            {
                goto Finished;
            }

            hr = GetStandaloneHostfxrParameters(struExeLocation.QueryStr(), pConfig);
            goto Finished;
        }
    }
    else
    {
        if (FAILED(hr = HOSTFXR_UTILITY::CallWhere(&struExeLocation))) {
            goto Finished;
        }

    }

    if (FAILED(hr = struExeLocation.SyncWithBuffer()) ||
        FAILED(hr = struHostFxrPath.Copy(struExeLocation)))
    {
        goto Finished;
    }

    dwPosition = struHostFxrPath.LastIndexOf(L'\\', 0);
    if (dwPosition == -1)
    {
        hr = E_FAIL;
        goto Finished;
    }

    struHostFxrPath.QueryStr()[dwPosition] = L'\0';

    if (FAILED(hr = struHostFxrPath.SyncWithBuffer()) ||
        FAILED(hr = struHostFxrPath.Append(L"\\")))
    {
        goto Finished;
    }

    hr = struHostFxrPath.Append(L"host\\fxr");
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (!UTILITY::DirectoryExists(&struHostFxrPath))
    {
        // error, not found in folder
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Find all folders under host\\fxr\\ for version numbers.
    hr = struHostFxrSearchExpression.Copy(struHostFxrPath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = struHostFxrSearchExpression.Append(L"\\*");
    if (FAILED(hr))
    {
        goto Finished;
    }

    // As we use the logic from core-setup, we are opting to use std here.
    // TODO remove all uses of std?
    UTILITY::FindDotNetFolders(struHostFxrSearchExpression.QueryStr(), &vVersionFolders);

    if (vVersionFolders.size() == 0)
    {
        // no core framework was found
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    hr = UTILITY::FindHighestDotNetVersion(vVersionFolders, &struHighestDotnetVersion);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struHostFxrPath.Append(L"\\"))
        || FAILED(hr = struHostFxrPath.Append(struHighestDotnetVersion.QueryStr()))
        || FAILED(hr = struHostFxrPath.Append(L"\\hostfxr.dll")))
    {
        goto Finished;
    }

    if (!UTILITY::CheckIfFileExists(struHostFxrPath.QueryStr()))
    {
        hr = ERROR_FILE_INVALID;
        goto Finished;
    }

    if (FAILED(hr = SetHostFxrArguments(pConfig->QueryArguments()->QueryStr(), struExeLocation.QueryStr(), pConfig)))
    {
        goto Finished;
    }

    if (FAILED(hr = pConfig->SetHostFxrFullPath(struHostFxrPath.QueryStr())))
    {
        goto Finished;
    }

Finished:

    return hr;
}

//
// Forms the argument list in HOSTFXR_PARAMETERS.
// Sets the ArgCount and Arguments.
// Arg structure:
// argv[0] = Path to exe activating hostfxr.
// argv[1] = L"exec"
// argv[2] = absolute path to dll. 
// 
HRESULT
HOSTFXR_UTILITY::SetHostFxrArguments(
    PCWSTR pwzArgumentsFromConfig,
    PCWSTR pwzExePath,
    ASPNETCORE_CONFIG* pConfig
)
{
    HRESULT     hr = S_OK;
    INT         argc = 0;
    PCWSTR*     argv = NULL;
    LPWSTR*     pwzArgs = NULL;
    STRU        struTempPath;
    DWORD         dwArgsProcessed = 0;

    pwzArgs = CommandLineToArgvW(pwzArgumentsFromConfig, &argc);

    if (pwzArgs == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    if (argc < 1)
    {
        // Invalid arguments
        hr = E_INVALIDARG;
        goto Failure;
    }

    argv = new PCWSTR[argc + 2];
    if (argv == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }

    argv[0] = SysAllocString(pwzExePath);

    if (argv[0] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    argv[1] = SysAllocString(L"exec");
    if (argv[1] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    // Try to convert the application dll from a relative to an absolute path
    // Don't record this failure as pwzArgs[0] may already be an absolute path to the dll.
    if (SUCCEEDED(UTILITY::ConvertPathToFullPath(pwzArgs[0], pConfig->QueryApplicationPhysicalPath()->QueryStr(), &struTempPath)))
    {
        argv[2] = SysAllocString(struTempPath.QueryStr());
    }
    else
    {
        argv[2] = SysAllocString(pwzArgs[0]);
    }
    if (argv[2] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    for (INT i = 1; i < argc; i++)
    {
        argv[i + 2] = SysAllocString(pwzArgs[i]);
        if (argv[i + 2] == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failure;
        }
        dwArgsProcessed++;
    }

    pConfig->SetHostFxrArguments(argc + 2, argv);
    goto Finished;

    Failure:
    if (argv != NULL)
    {
        for (DWORD i = 0; i < dwArgsProcessed; i++)
        {
            SysFreeString((BSTR)argv[i]);
        }
    }

    delete[] argv;

    Finished:
    if (pwzArgs != NULL)
    {
        LocalFree(pwzArgs);
        DBG_ASSERT(pwzArgs == NULL);
    }
    return hr;
}

HRESULT
HOSTFXR_UTILITY::CallWhere(_Out_ STRU* struDotnetLocation)
{
    HRESULT hr = S_OK;
    STARTUPINFOW            startupInfo = { 0 };
    PROCESS_INFORMATION     processInformation = { 0 };
    STRU struTempPath;
    SECURITY_ATTRIBUTES sa;
    HANDLE hFile = NULL;
    DWORD exitCode;
    LPWSTR dotnetName;
    DWORD  numBytesRead;
    BOOL result;
    DWORD dwRetVal;
    STRU struTempFilePath;
    CHAR dotnetLocations[4000] = { 0 };
    STRU dotnetLocationsString;
    startupInfo.cb = sizeof(startupInfo);
    INT index = 0;
    INT prevIndex = 0;
    STRU dotnetSubstring;
    BOOL fIsWow64Process;
    BOOL fIsCurrentProcess64Bit;
    DWORD dwBinaryType;
    BOOL fFound = FALSE;
    DWORD dwfilePointer;

    if (FAILED(hr = struTempPath.Resize(MAX_PATH)))
    {
        goto Finished;
    }
    // Get a temporary path/file to write the results of where.exe
    dwRetVal = GetTempPathW(MAX_PATH, struTempPath.QueryStr());
    if (dwRetVal == 0 || dwRetVal > MAX_PATH)
    {
        hr = ERROR_PATH_NOT_FOUND;
        goto Finished;
    }

    if (FAILED(hr = struTempPath.SyncWithBuffer()))
    {
        goto Finished;
    }

    if (FAILED(hr = struTempFilePath.Resize(MAX_PATH + 50)))
    {
        goto Finished;
    }
    dwRetVal = GetTempFileNameW(struTempPath.QueryStr(),
        L"ANCM_WHERE",
        0,
        struTempFilePath.QueryStr());

    if (FAILED(hr = struTempFilePath.SyncWithBuffer()))
    {
        goto Finished;
    }

    sa.nLength = sizeof(sa);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle = TRUE;

    hFile = CreateFile(struTempFilePath.QueryStr(),               // file name 
        GENERIC_ALL,
        FILE_SHARE_WRITE | FILE_SHARE_READ,
        &sa,                  // default security 
        OPEN_ALWAYS,         // existing file only 
        FILE_ATTRIBUTE_NORMAL, // normal file 
        NULL);                 // no template 

    if (hFile == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
    }
    startupInfo.dwFlags |= STARTF_USESTDHANDLES;
    startupInfo.hStdOutput = hFile;
    startupInfo.hStdError = hFile;

    dotnetName = SysAllocString(L"\"C:\\Windows\\System32\\where.exe\" dotnet.exe");

    result = CreateProcessW(NULL,
        dotnetName,
        NULL,
        NULL,
        TRUE,
        CREATE_NO_WINDOW,
        NULL,
        NULL,
        &startupInfo,
        &processInformation
    );

    if (!result)
    {
        printf("CreateProcess failed (%d).\n", GetLastError());
        return 1;
    }

    WaitForSingleObject(processInformation.hProcess, 1000); // TODO set this to be timeout based on config. 

    result = GetExitCodeProcess(processInformation.hProcess, &exitCode);

    CloseHandle(processInformation.hProcess);
    CloseHandle(processInformation.hThread);

    SysFreeString(dotnetName);

    dwfilePointer = SetFilePointer(hFile, 0, NULL, FILE_BEGIN);
    if (dwfilePointer == INVALID_SET_FILE_POINTER)
    {
        hr = ERROR_FILE_INVALID;
        goto Finished;
    }

    if (!ReadFile(hFile, dotnetLocations, 4096, &numBytesRead, NULL))
    {
        goto Finished;
    }
    dotnetLocationsString.CopyA(dotnetLocations, numBytesRead);

    // Go through each line of the file, check if the path is valid.
    // Log which dotnet exe we are using to stdout before invoking dotnet.exe
    // Add good error message saying if this dotnet isn't the one you intended,
    // make sure the bitness matches.
    if (!IsWow64Process(GetCurrentProcess(), &fIsWow64Process))
    {
        // Calling IsWow64Process failed
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
    if (fIsWow64Process)
    {
        // 32 bit mode
        fIsCurrentProcess64Bit = FALSE;
    }
    else
    {
        SYSTEM_INFO systemInfo;
        GetNativeSystemInfo(&systemInfo);
        fIsCurrentProcess64Bit = systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64;
    }

    while (!fFound)
    {
        //
        index = dotnetLocationsString.IndexOf(L"\r\n", prevIndex);
        if (index == -1)
        {
            break;
        }
        dotnetSubstring.Copy(dotnetLocationsString.QueryStr(), index - prevIndex);
        prevIndex = index;

        if (!GetBinaryTypeW(dotnetSubstring.QueryStr(), &dwBinaryType))
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
        if (fIsCurrentProcess64Bit == (dwBinaryType == SCS_64BIT_BINARY)) {
            // Found a valid dotnet.
            struDotnetLocation->Copy(dotnetSubstring);
            fFound = TRUE;
        }
    }

    if (!fFound)
    {
        hr = ERROR_FILE_NOT_FOUND;
        goto Finished;
    }
Finished:

    if (hFile != NULL)
    {
        if (!CloseHandle(hFile)) 
        {
            // TODO log warning.
        }
    }

    return hr;
}
