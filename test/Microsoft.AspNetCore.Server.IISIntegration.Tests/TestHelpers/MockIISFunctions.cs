using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tests
{
    /// <summary>
    /// Represents a mock of ANCM in process.  
    /// </summary>
    /// <remarks>
    /// Call SendAsync with a HttpContext. Response will be added to the context. 
    /// </remarks>
    internal unsafe class MockIISFunctions : IISFunctions
    {
        // Mapping from request to test context. IntPtrs are used as unique identifiers for 
        internal ConcurrentDictionary<IntPtr, TestIISContext> IISRequestContextDictionary = new ConcurrentDictionary<IntPtr, TestIISContext>();

        private int _cbBytes = 0;
        private int _hr = 0;
       
        public IISDelegates.PFN_REQUEST_HANDLER RequestCallback { get; set; } = null;
        public IISDelegates.PFN_SHUTDOWN_HANDLER ShutdownCallback { get; set; } = null;
        private IISDelegates.PFN_ASYNC_COMPLETION AsyncCallback { get; set; } = null;

        private IntPtr _pvRequestContext;
        private IntPtr _pvShutdownContext;

        public MockIISFunctions()
        {
        }

        public void AbortRequest(IntPtr pHttpContext)
        {
            //_abort = true;
            throw new NotImplementedException();
        }

        public void CancelIo(IntPtr pHttpContext)
        {
            throw new NotImplementedException();
        }

        public void EnableWebsockets(IntPtr pHttpContext)
        {
        }

        public int FlushResponseBytes(IntPtr pHttpContext, out bool fCompletionExpected)
        {
            fCompletionExpected = false;
            return 0;
        }

        public void GetCompletionInfo(IntPtr pCompletionInfo, out int cbBytes, out int hr)
        {
            cbBytes = _cbBytes;
            hr = _hr;
        }

        public unsafe HttpApiTypes.HTTP_REQUEST_V2* GetRawRequest(IntPtr pHttpContext)
        {
            // Need to pin this native struct.
            var testIISContext = IISRequestContextDictionary[pHttpContext];
            var handle = GCHandle.Alloc(testIISContext.NativeRequest, GCHandleType.Pinned);
            testIISContext.GCHandles.Add(handle);

            return (HttpApiTypes.HTTP_REQUEST_V2*)handle.AddrOfPinnedObject();
        }

        public unsafe HttpApiTypes.HTTP_RESPONSE_V2* GetRawResponse(IntPtr pHttpContext)
        {
            var testIISContext = IISRequestContextDictionary[pHttpContext];
            var handle = GCHandle.Alloc(testIISContext.NativeResponse, GCHandleType.Pinned);
            testIISContext.GCHandles.Add(handle);

            return (HttpApiTypes.HTTP_RESPONSE_V2*)handle.AddrOfPinnedObject();
        }

        public void IndicateCompletion(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS notificationStatus)
        {
            // Not used
            throw new NotImplementedException();
        }

        public void PostCompletion(IntPtr pHttpContext, int cbBytes)
        {
            IISRequestContextDictionary[pHttpContext].CancellationTokenSource.Cancel();
        }

        public unsafe int ReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, out int dwBytesReceived, out bool fCompletionExpected)
        {
            // Assume no body for now.
            var request = IISRequestContextDictionary[pHttpContext];
            dwBytesReceived = 0;
            fCompletionExpected = false;
            return 0;
        }

        public unsafe int WriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, out bool fCompletionExpected)
        {
            // Synchrnous Writes for now.
            var response = IISRequestContextDictionary[pHttpContext].Context.Response;
            for (var i = 0; i < nChunks; i++)
            {
                var chunk = pDataChunks[i];
                var managedArray = new byte[chunk.fromMemory.BufferLength];
                Marshal.Copy(managedArray, 0, chunk.fromMemory.pBuffer, (int)chunk.fromMemory.BufferLength);
                response.Body.Write(managedArray, 0, managedArray.Length);
            }
            fCompletionExpected = false;
            return 0;
        }

        public void RegisterCallbacks(IISDelegates.PFN_REQUEST_HANDLER request_callback, IISDelegates.PFN_SHUTDOWN_HANDLER shutdown_callback, IISDelegates.PFN_ASYNC_COMPLETION async_callback, IntPtr pvRequestContext, IntPtr pvShutdownContext)
        {
            RequestCallback = request_callback;
            ShutdownCallback = shutdown_callback;
            AsyncCallback = async_callback;
            _pvRequestContext = pvRequestContext;
            _pvShutdownContext = pvShutdownContext;
        }

        public void SetCompletionStatus(IntPtr pHttpContext, REQUEST_NOTIFICATION_STATUS requestNotificationStatus)
        {
            IISRequestContextDictionary[pHttpContext].RequestNotificationStatus = requestNotificationStatus;
        }

        public void SetManagedContext(IntPtr pHttpContext, IntPtr pvManagedContext)
        {
            IISRequestContextDictionary[pHttpContext].ManagedServerPointer = pvManagedContext;
        }

        public unsafe void SetResponseStatusCode(IntPtr pHttpContext, ushort statusCode, byte* pszReason)
        {
            IISRequestContextDictionary[pHttpContext].Context.Response.StatusCode = statusCode;
        }

        public void Shutdown()
        {
        }

        public unsafe int WebsocketsReadRequestBytes(IntPtr pHttpContext, byte* pvBuffer, int cbBuffer, IISDelegates.PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out int dwBytesReceived, out bool fCompletionExpected)
        {
            throw new NotImplementedException();
        }

        public unsafe int WebsocketsWriteResponseBytes(IntPtr pHttpContext, HttpApiTypes.HTTP_DATA_CHUNK* pDataChunks, int nChunks, IISDelegates.PFN_WEBSOCKET_ASYNC_COMPLETION pfnCompletionCallback, IntPtr pvCompletionContext, out bool fCompletionExpected)
        {
            throw new NotImplementedException();
        }
    }
}
