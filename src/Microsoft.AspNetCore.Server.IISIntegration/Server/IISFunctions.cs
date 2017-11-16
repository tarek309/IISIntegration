using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;
using static Microsoft.AspNetCore.Server.IISIntegration.IISDelegates;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal unsafe interface IISFunctions
    {
        void PostCompletion(IntPtr pHttpContext, int cbBytes);
        void SetCompletionStatus(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS requestNotificationStatus);
        void IndicateCompletion(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS notificationStatus);
        int ReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected);
        void RegisterCallbacks(PFN_REQUEST_HANDLER request_callback, PFN_SHUTDOWN_HANDLER shutdown_callback, PFN_ASYNC_COMPLETION async_callback, IntPtr pvRequestContext, IntPtr pvShutdownContext);
        int WriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected);
        int FlushResponseBytes(IntPtr pHttpContext, out bool fCompletionExpected);
        HttpApiTypes.HTTP_REQUEST_V2* GetRawRequest(IntPtr pHttpContext);
        HttpApiTypes.HTTP_RESPONSE_V2* GetRawResponse(IntPtr pHttpContext);
        void SetResponseStatusCode(IntPtr pHttpContext, ushort statusCode, byte* pszReason);
        void GetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr);
        void SetManagedContext(IntPtr pHttpContext, IntPtr pvManagedContext);
        void Shutdown();
        int WebsocketsReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected);
        int WebsocketsWriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected);
        void EnableWebsockets(IntPtr pHttpContext);
        void CancelIo(IntPtr pHttpContext);
        void AbortRequest(IntPtr pHttpContext);
        void SetKnownResponseHeader(IntPtr pHttpContext, int headerId, byte* pHeaderValue, ushort length, bool fReplace);
        void SetUnknownResponseHeader(IntPtr pHttpContext, byte* pszHeaderName, byte* pszHeaderValue, ushort usHeaderValueLength, bool fReplace);
        void GetAuthenticationInformation(IntPtr pHttpContext, [MarshalAs(UnmanagedType.BStr)] out string authType, out IntPtr token);
    }
}
