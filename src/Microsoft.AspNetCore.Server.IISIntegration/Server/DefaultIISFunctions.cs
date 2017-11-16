using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.HttpSys.Internal;
using static Microsoft.AspNetCore.Server.IISIntegration.IISDelegates;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal unsafe class DefaultIISFunctions : IISFunctions
    {
        public DefaultIISFunctions()
        {
        }

        internal const int S_OK = 0;
        private const string AspNetCoreModuleDll = "aspnetcore.dll";

        internal static void ThrowExceptionIfErrored(int hResult)
        {
            if (hResult != S_OK)
            {
                throw new IISServerException(hResult);
            }
        }

        internal static Exception GetExceptionIfErrored(int hResult)
        {
            if (hResult != S_OK)
            {
                return new IISServerException(hResult);
            }
            return null;
        }

        public void PostCompletion(IntPtr pHttpContext, int cbBytes)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_post_completion(pHttpContext, cbBytes));
        }

        public void SetCompletionStatus(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS requestNotificationStatus)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_set_completion_status(pHttpContext, requestNotificationStatus));
        }

        public void IndicateCompletion(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS notificationStatus)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_indicate_completion(pHttpContext, notificationStatus));
        }

        public void RegisterCallbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, PFN_ASYNC_COMPLETION async_callback, IntPtr pvRequestContext, IntPtr pvShutdownContext)
        {
            IISNativeMethods.register_callbacks(request_callback, shutdown_callback, async_callback, pvRequestContext, pvShutdownContext);
        }

        public int ReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return IISNativeMethods.http_read_request_bytes(pHttpContext, pvBuffer, cbBuffer, out dwBytesReceived, out fCompletionExpected);
        }

        public int WriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            return IISNativeMethods.http_write_response_bytes(pHttpContext, pDataChunks, nChunks, out fCompletionExpected);
        }

        public int FlushResponseBytes(IntPtr pHttpContext, out bool fCompletionExpected)
        {
            return IISNativeMethods.http_flush_response_bytes(pHttpContext, out fCompletionExpected);
        }

        public HttpApiTypes.HTTP_REQUEST_V2* GetRawRequest(IntPtr pHttpContext)
        {
            return IISNativeMethods.http_get_raw_request(pHttpContext);
        }

        public HttpApiTypes.HTTP_RESPONSE_V2* GetRawResponse(IntPtr pHttpContext)
        {
            return IISNativeMethods.http_get_raw_response(pHttpContext);
        }

        public void SetResponseStatusCode(IntPtr pHttpContext, ushort statusCode, byte* pszReason)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_set_response_status_code(pHttpContext, statusCode, pszReason));
        }

        public void GetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_get_completion_info(pCompletionInfo, out cbBytes, out hr));
        }


        public void SetManagedContext(IntPtr pHttpContext, IntPtr pvManagedContext)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_set_managed_context(pHttpContext, pvManagedContext));
        }

        public void Shutdown()
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_shutdown());
        }

        public int WebsocketsReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected)
        {
            return IISNativeMethods.http_websockets_read_bytes(pHttpContext, pvBuffer, cbBuffer, pfnCompletionCallback, pvCompletionContext, out dwBytesReceived, out fCompletionExpected);
        }

        public int WebsocketsWriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected)
        {
            return IISNativeMethods.http_websockets_write_bytes(pHttpContext, pDataChunks, nChunks, pfnCompletionCallback, pvCompletionContext, out fCompletionExpected);
        }

        public void EnableWebsockets(IntPtr pHttpContext)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_enable_websockets(pHttpContext));
        }

        public void CancelIo(IntPtr pHttpContext)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_cancel_io(pHttpContext));
        }

        public void AbortRequest(IntPtr pHttpContext)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_abort_request(pHttpContext));
        }

        public void SetKnownResponseHeader(IntPtr pHttpContext, int headerId, byte* pHeaderValue, ushort length, bool fReplace)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_response_set_known_header(pHttpContext, headerId, pHeaderValue, length, fReplace));
        }

        public void SetUnknownResponseHeader(IntPtr pHttpContext, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_response_set_unknown_header(pHttpContext, pszHeaderName, pszHeaderValue, usHeaderValueLength, fReplace));
        }

        public void GetAuthenticationInformation(IntPtr pHttpContext, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_get_authentication_information(pHttpContext, out authType, out token));
        }

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        internal static bool IsAncmLoaded()
        {
            return GetModuleHandle(AspNetCoreModuleDll) != IntPtr.Zero;
        }

        internal static void IISGetApplicationProperties(ref IISConfigurationData configurationData)
        {
            ThrowExceptionIfErrored(IISNativeMethods.http_get_application_properties(ref configurationData));
        }

        private class IISNativeMethods
        {
            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_post_completion(IntPtr pHttpContext, int cbBytes);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_set_completion_status(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS requestNotificationStatus);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_indicate_completion(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS notificationStatus);

            [DllImport(AspNetCoreModuleDll)]
            public static extern void register_callbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, PFN_ASYNC_COMPLETION async_callback, IntPtr pvRequestContext, IntPtr pvShutdownContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_read_request_bytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_write_response_bytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_flush_response_bytes(IntPtr pHttpContext, out bool fCompletionExpected);

            [DllImport(AspNetCoreModuleDll)]
            public static extern HttpApiTypes.HTTP_REQUEST_V2* http_get_raw_request(IntPtr pHttpContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern HttpApiTypes.HTTP_RESPONSE_V2* http_get_raw_response(IntPtr pHttpContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_set_response_status_code(IntPtr pHttpContext, ushort statusCode, byte* pszReason);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_get_completion_info(IntPtr pCompletionInfo, out int cbBytes, out int hr);

            [DllImport(AspNetCoreModuleDll)]
            internal static extern int http_set_managed_context(IntPtr pHttpContext, IntPtr pvManagedContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_get_application_properties(ref IISConfigurationData configurationData);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_shutdown();

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_websockets_read_bytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_websockets_write_bytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_enable_websockets(IntPtr pHttpContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_cancel_io(IntPtr pHttpContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_abort_request(IntPtr pHttpContext);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_response_set_unknown_header(IntPtr pHttpContext, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);

            [DllImport(AspNetCoreModuleDll)]
            internal static extern int http_response_set_known_header(IntPtr pHttpContext, int headerId, byte* pHeaderValue, ushort length, bool fReplace);

            [DllImport(AspNetCoreModuleDll)]
            public static extern int http_get_authentication_information(IntPtr pHttpContext, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);
        }
    }
}
