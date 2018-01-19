// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define IDS_INVALID_PROPERTY        1000
#define IDS_SERVER_ERROR            1001

#define ASPNETCORE_EVENT_PROVIDER L"IIS AspNetCore Module"
#define ASPNETCORE_IISEXPRESS_EVENT_PROVIDER L"IIS Express AspNetCore Module"

#define ASPNETCORE_EVENT_MSG_BUFFER_SIZE 256
#define ASPNETCORE_EVENT_PROCESS_START_SUCCESS_MSG           L"Application '%s' started process '%d' successfully and is listening on port '%d'."
#define ASPNETCORE_EVENT_RAPID_FAIL_COUNT_EXCEEDED_MSG       L"Maximum rapid fail count per minute of '%d' exceeded."
#define ASPNETCORE_EVENT_PROCESS_START_INTERNAL_ERROR_MSG    L"Application '%s' failed to parse processPath and arguments due to internal error, ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_PROCESS_START_POSTCREATE_ERROR_MSG  L"Application '%s' with physical root '%s' created process with commandline '%s'but failed to get its status, ErrorCode = '0x%x', retryCounter '%d'."
#define ASPNETCORE_EVENT_PROCESS_START_ERROR_MSG             L"Application '%s' with physical root '%s' failed to start process with commandline '%s', ErrorCode = '0x%x', retryCounter '%d'."
#define ASPNETCORE_EVENT_PROCESS_START_WRONGPORT_ERROR_MSG   L"Application '%s' with physical root '%s' created process with commandline '%s' but failed to listen on the given port '%d'"
#define ASPNETCORE_EVENT_PROCESS_START_NOTREADY_ERROR_MSG    L"Application '%s' with physical root '%s' created process with commandline '%s' but either crashed or did not respond or did not listen on the given port '%d', ErrorCode = '0x%x'"
#define ASPNETCORE_EVENT_INVALID_STDOUT_LOG_FILE_MSG         L"Warning: Could not create stdoutLogFile %s, ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_GRACEFUL_SHUTDOWN_FAILURE_MSG       L"Failed to gracefully shutdown process '%d'."
#define ASPNETCORE_EVENT_SENT_SHUTDOWN_HTTP_REQUEST_MSG      L"Sent shutdown HTTP message to process '%d' and received http status '%d'."
#define ASPNETCORE_EVENT_LOAD_CLR_FALIURE_MSG                L"Application '%s' with physical root '%s' failed to load clr and managed application, ErrorCode = '0x%x."
#define ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG        L"Only one inprocess application is allowed per IIS application pool. Please assign the application '%s' to a different IIS application pool."
#define ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG       L"Mixed hosting model is not supported. Application '%s' configured with different hostingModel value '%d' other than the one of running application(s)."
#define ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG           L"Failed to start application '%s', ErrorCode '0x%x'."
#define ASPNETCORE_EVENT_INPROCESS_THREAD_EXIT_MSG           L"Application '%s' with physical root '%s' hit unexpected managed background thread exit, ErrorCode = '0x%x. Please check the stderr logs for more information."
#define ASPNETCORE_EVENT_RECYCLE_APPOFFLINE_MSG              L"Application '%s' is recycled due to app_offline file was detected."
#define ASPNETCORE_EVENT_MODULE_DISABLED_MSG                 L"AspNetCore Module is disabled"
#define ASPNETCORE_EVENT_INPROCESS_FULL_FRAMEWORK_APP_MSG    L"Application '%s' was compiled for .NET Framework. Please compile for .NET core to run the inprocess application or change the process mode to out of process. ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_PORTABLE_APP_DOTNET_MISSING_MSG     L"Could not find dotnet.exe on the system PATH environment variable for portable application '%s'. Check that a valid path to dotnet is on the PATH and the bitness of dotnet matches the bitness of the IIS worker process. ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_HOSTFXR_DIRECTORY_NOT_FOUND_MSG     L"Could not find the hostfxr directory '%s' in the dotnet directory. ErrorCode = '0x%x'."
#define ASPNETCORE_EVENT_HOSTFXR_DLL_NOT_FOUND_MSG           L"Could not find hostfxr.dll in '%s'. ErrorCode = '0x%x'."
