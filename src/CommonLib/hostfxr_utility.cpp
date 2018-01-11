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
    STRU*              pStruExePath,
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

    if (FAILED(hr = struDllPath.Copy(*pStruExePath)))
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

    if (FAILED(hr = SetHostFxrArguments(&struArguments, pStruExePath, pConfig)))
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
    DWORD                       dwPathLength = MAX_PATH;
    DWORD                       dwDotnetLength = 0;
    BOOL                        fFound = FALSE;

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
        // Check if hostfxr is in this folder, if it is, we are a standalone application.
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

            hr = GetStandaloneHostfxrParameters(&struExeLocation, pConfig);
            goto Finished;
        }
    }

    // Find dotnet.exe on the path.
    struExeLocation.Reset();

    if (FAILED(hr = struExeLocation.Resize(MAX_PATH)))
    {
        goto Finished;
    }

    while (!fFound)
    {
        dwDotnetLength = SearchPath(NULL, L"dotnet", L".exe", dwPathLength, struExeLocation.QueryStr(), NULL);
        if (dwDotnetLength == 0)
        {
            hr = GetLastError();
            // Could not find dotnet
            goto Finished;
        }
        else if (dwDotnetLength == dwPathLength)
        {
            // Increase size
            dwPathLength *= 2;
            if (FAILED(hr = struExeLocation.Resize(dwPathLength)))
            {
                goto Finished;
            }
        }
        else
        {
            fFound = TRUE;
        }
    }

    if (FAILED(hr = struExeLocation.SyncWithBuffer())
        || FAILED(hr = struHostFxrPath.Copy(struExeLocation)))
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

    if (FAILED(hr = struHostFxrPath.SyncWithBuffer())
        || FAILED(hr = struHostFxrPath.Append(L"\\")))
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

    if (FAILED(hr = SetHostFxrArguments(pConfig->QueryArguments(), &struExeLocation, pConfig)))
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
// argv[2] = first argument specified in the arguments portion of aspnetcore config. 
// 
HRESULT
HOSTFXR_UTILITY::SetHostFxrArguments(
    STRU* struArgumentsFromConfig,
    STRU* pstruExePath,
    ASPNETCORE_CONFIG* pConfig
)
{
    HRESULT     hr = S_OK;
    INT         argc = 0;
    PCWSTR*     argv = NULL;
    LPWSTR*     pwzArgs = NULL;
    STRU        struTempPath;

    pwzArgs = CommandLineToArgvW(struArgumentsFromConfig->QueryStr(), &argc);

    if (pwzArgs == NULL || argc < 1)
    {
        goto Finished;
    }

    // Try to convert the application dll from a relative to an absolute path
    // Don't record this failure as pwzArgs[0] may already be an absolute path to the dll.
    if (!FAILED(UTILITY::ConvertPathToFullPath(pwzArgs[0], pConfig->QueryApplicationPhysicalPath()->QueryStr(), &struTempPath)))
    {
        pwzArgs[0] = struTempPath.QueryStr();
    }

    argv = new PCWSTR[argc + 2];
    if (argv == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    argv[0] = SysAllocString(pstruExePath->QueryStr());
    argv[1] = SysAllocString(L"exec");

    for (INT i = 0; i < argc; i++)
    {
        argv[i + 2] = SysAllocString(pwzArgs[i]);
    }

    pConfig->SetHostFxrArguments(argc + 2, argv);

Finished:
    if (pwzArgs != NULL)
    {
        LocalFree(pwzArgs);
        DBG_ASSERT(pwzArgs == NULL);
    }
    return hr;
}
