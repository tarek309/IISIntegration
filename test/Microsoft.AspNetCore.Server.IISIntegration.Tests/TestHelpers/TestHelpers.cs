using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tests
{
    internal class TestHelpers
    {
        private static int _chunkSize = 2048;
        private static int _requestCount = 0;

        public static async Task SendRequest(MockIISFunctions iisFunctions, HttpContext httpContext, IntPtr serverPointer)
        {
            // Providing unique identifier for request.
            var pHttpContext = new IntPtr(Interlocked.Increment(ref _requestCount));
            CreateNativeRequestFromHttpRequest(httpContext, iisFunctions, pHttpContext, serverPointer);
            var result = iisFunctions.RequestCallback(pHttpContext, serverPointer);
            // await PostCompletion to be called. 
            // TODO return result.
            if (result == REQUEST_NOTIFICATION_STATUS.RQ_NOTIFICATION_PENDING)
            {
                try
                {
                    await iisFunctions.IISRequestContextDictionary[pHttpContext].CompleteRequest;
                }
                catch (TaskCanceledException)
                {

                }
            }
        }

        private static unsafe void CreateNativeRequestFromHttpRequest(HttpContext httpContext, MockIISFunctions iisFunctions, IntPtr pHttpContext, IntPtr serverPointer)
        {
            var managedRequest = httpContext.Request;
            iisFunctions.IISRequestContextDictionary[pHttpContext] = new TestIISContext();

            var nativeRequest = new HttpApiTypes.HTTP_REQUEST_V2();

            nativeRequest.Request.Flags = 0;

            nativeRequest.Request.Version.MajorVersion = 1;
            nativeRequest.Request.Version.MinorVersion = 1;
            nativeRequest.Request.RequestId = (ulong)pHttpContext.ToInt32();
            nativeRequest.Request.RawConnectionId = (ulong)pHttpContext.ToInt32();

            nativeRequest.Request.Verb = HttpVerbFromHttpRequest(managedRequest);
            var gcHandles = new List<GCHandle>();

            // Url information
            GCHandle gcHandle;
            try
            {
                // TODO none of these may work because of pointers to 16 bit instead of 8 bit
                var host = managedRequest.Host.Value ?? "";
                gcHandle = GCHandle.Alloc(host, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.CookedUrl.pHost = (ushort*)gcHandle.AddrOfPinnedObject();
                nativeRequest.Request.CookedUrl.HostLength = (ushort)host.Length;

                var absPath = managedRequest.Path.Value ?? "";
                gcHandle = GCHandle.Alloc(absPath, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.CookedUrl.pAbsPath = (ushort*)gcHandle.AddrOfPinnedObject();
                nativeRequest.Request.CookedUrl.AbsPathLength = (ushort)absPath.Length;

                var queryString = managedRequest.QueryString.Value ?? "";
                gcHandle = GCHandle.Alloc(queryString, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.CookedUrl.pQueryString = (ushort*)gcHandle.AddrOfPinnedObject();
                nativeRequest.Request.CookedUrl.QueryStringLength = (ushort)queryString.Length;

                var fullUrl = UriHelper.BuildAbsolute(managedRequest.Scheme, managedRequest.Host, managedRequest.PathBase, managedRequest.Path, managedRequest.QueryString);
                gcHandle = GCHandle.Alloc(fullUrl, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.CookedUrl.pFullUrl = (ushort*)gcHandle.AddrOfPinnedObject();
                nativeRequest.Request.CookedUrl.FullUrlLength = (ushort)fullUrl.Length;

                // RawUrl is what is in the request line
                var rawUrl = Encoding.ASCII.GetBytes(managedRequest.Path.Value ?? "");
                gcHandle = GCHandle.Alloc(rawUrl, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.pRawUrl = (byte*)gcHandle.AddrOfPinnedObject();
                nativeRequest.Request.RawUrlLength = (ushort)rawUrl.Length;


                // Headers
                HttpApiTypes.HTTP_UNKNOWN_HEADER[] unknownHeaders = null;

                string headerName;
                string headerValue;
                int lookup;
                var numUnknownHeaders = 0;
                int numKnownMultiHeaders = 0;
                byte[] bytes = null;

                foreach (var headerPair in managedRequest.Headers)
                {
                    if (headerPair.Value.Count == 0)
                    {
                        continue;
                    }
                    lookup = KnownHeaderFromRequestHeader(headerPair.Key);
                    if (lookup == -1) // TODO handle opaque stream upgrade?
                    {
                        numUnknownHeaders++;
                    }
                    else if (headerPair.Value.Count > 1)
                    {
                        numKnownMultiHeaders++;
                    }
                }

                var pKnownHeaders = &nativeRequest.Request.Headers.KnownHeaders;
                foreach (var headerPair in managedRequest.Headers)
                {
                    if (headerPair.Value.Count == 0)
                    {
                        continue;
                    }
                    headerName = headerPair.Key;
                    StringValues headerValues = headerPair.Value;
                    lookup = KnownHeaderFromRequestHeader(headerName);
                    if (lookup == -1)
                    {
                        if (unknownHeaders == null)
                        {
                            unknownHeaders = new HttpApiTypes.HTTP_UNKNOWN_HEADER[numUnknownHeaders];
                            gcHandle = GCHandle.Alloc(unknownHeaders, GCHandleType.Pinned);
                            gcHandles.Add(gcHandle);
                            nativeRequest.Request.Headers.pUnknownHeaders = (HttpApiTypes.HTTP_UNKNOWN_HEADER*)gcHandle.AddrOfPinnedObject();
                            nativeRequest.Request.Headers.UnknownHeaderCount = 0; // to remove the iis header for server=...
                        }

                        for (var headerValueIndex = 0; headerValueIndex < headerValues.Count; headerValueIndex++)
                        {
                            // Add Name
                            bytes = HeaderEncoding.GetBytes(headerName);
                            unknownHeaders[nativeRequest.Request.Headers.UnknownHeaderCount].NameLength = (ushort)bytes.Length;
                            gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            gcHandles.Add(gcHandle);
                            unknownHeaders[nativeRequest.Request.Headers.UnknownHeaderCount].pName = (byte*)gcHandle.AddrOfPinnedObject();

                            // Add Value
                            headerValue = headerValues[headerValueIndex] ?? string.Empty;
                            bytes = HeaderEncoding.GetBytes(headerValue);
                            unknownHeaders[nativeRequest.Request.Headers.UnknownHeaderCount].RawValueLength = (ushort)bytes.Length;
                            gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                            gcHandles.Add(gcHandle);
                            unknownHeaders[nativeRequest.Request.Headers.UnknownHeaderCount].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                            nativeRequest.Request.Headers.UnknownHeaderCount++;
                        }
                    }
                    else if (headerPair.Value.Count == 1)
                    {
                        headerValue = headerValues[0] ?? string.Empty;
                        bytes = HeaderEncoding.GetBytes(headerValue);
                        pKnownHeaders[lookup].RawValueLength = (ushort)bytes.Length;
                        gcHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                        gcHandles.Add(gcHandle);
                        pKnownHeaders[lookup].pRawValue = (byte*)gcHandle.AddrOfPinnedObject();
                    }
                    else
                    {
                        throw new NotImplementedException("Mock IIS does not support multi headers");
                    }
                }

                // Body
                // Read the whole body into memory, create chunks and retain gc handles (for freeing).
                List<byte[]> bufferList = new List<byte[]>();
                byte[] buffer = new byte[_chunkSize];
                int dataRead;
                while ((dataRead = managedRequest.Body.Read(buffer, 0, _chunkSize)) > 0)
                {
                    bufferList.Add(buffer);
                    buffer = new byte[_chunkSize];
                }

                // Count the number of chunks.
                var numChunks = bufferList.Count;
                var dataChunks = new HttpApiTypes.HTTP_DATA_CHUNK[numChunks];
                for (var currentChunk = 0; currentChunk < numChunks; currentChunk++)
                {
                    var chunk = dataChunks[currentChunk];
                    chunk.DataChunkType = HttpApiTypes.HTTP_DATA_CHUNK_TYPE.HttpDataChunkFromMemory;

                    chunk.fromMemory.BufferLength = (uint)bufferList[currentChunk].Length;

                    gcHandle = GCHandle.Alloc(bufferList[currentChunk], GCHandleType.Pinned);
                    gcHandles.Add(gcHandle);
                    chunk.fromMemory.pBuffer = (IntPtr)gcHandle.AddrOfPinnedObject();

                    currentChunk++;
                }

                gcHandle = GCHandle.Alloc(dataChunks, GCHandleType.Pinned);
                gcHandles.Add(gcHandle);
                nativeRequest.Request.EntityChunkCount = (ushort)numChunks;
                nativeRequest.Request.pEntityChunks = (HttpApiTypes.HTTP_DATA_CHUNK*)gcHandle.AddrOfPinnedObject();

                iisFunctions.IISRequestContextDictionary[pHttpContext].NativeRequest = nativeRequest;
                iisFunctions.IISRequestContextDictionary[pHttpContext].ManagedServerPointer = serverPointer;
                iisFunctions.IISRequestContextDictionary[pHttpContext].GCHandles = gcHandles;
                iisFunctions.IISRequestContextDictionary[pHttpContext].NativeResponse = new HttpApiTypes.HTTP_RESPONSE_V2();
                iisFunctions.IISRequestContextDictionary[pHttpContext].CompleteRequest =
                    Task.Delay(Debugger.IsAttached ? int.MaxValue : 10000, iisFunctions.IISRequestContextDictionary[pHttpContext].CancellationTokenSource.Token);
                iisFunctions.IISRequestContextDictionary[pHttpContext].Context = httpContext;
            }
            catch (Exception)
            {
                FreePinnedObjects(iisFunctions, pHttpContext);
                throw;
            }
        }

        private static unsafe void FreePinnedObjects(MockIISFunctions iisFunctions, IntPtr pHttpContext)
        {
            var pinnedHeaders = iisFunctions.IISRequestContextDictionary[pHttpContext].GCHandles;
            if (pinnedHeaders != null)
            {
                foreach (GCHandle handle in pinnedHeaders)
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }
        }

        private static HttpApiTypes.HTTP_VERB HttpVerbFromHttpRequest(HttpRequest request)
        {
            if (request.Method.Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                return HttpApiTypes.HTTP_VERB.HttpVerbGET;
            }
            else if (request.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
            {
                return HttpApiTypes.HTTP_VERB.HttpVerbPOST;
            }
            else if (request.Method.Equals("delete", StringComparison.OrdinalIgnoreCase))
            {
                return HttpApiTypes.HTTP_VERB.HttpVerbDELETE;
            }
            else if (request.Method.Equals("put", StringComparison.OrdinalIgnoreCase))
            {
                return HttpApiTypes.HTTP_VERB.HttpVerbPUT;
            }
            else
            {
                throw new NotImplementedException("Mock IIS does not support http verb type");
            }
        }

        private static readonly string[] _requestHeaderStrings =
        {
            "Cache-Control",
            "Connection",
            "Date",
            "Keep-Alive",
            "Pragma",
            "Trailer" ,
            "Transfer-Encoding",
            "Upgrade",
            "Via",
            "Warning",
            "Allow",
            "Content-Length",
            "Content-Type",
            "Content-Encoding",
            "Content-Language",
            "Content-Location",
            "Content-Md5",
            "Content-Range",
            "Expires",
            "Last-Modified",

            "Accept",
            "Accept-Charset",
            "Accept-Encoding",
            "Accept-Language",
            "Authorization",
            "Cookie",
            "Expect",
            "From",
            "Host",
            "If-Match",
            "If-Modified-Since",
            "If-None-Match",
            "If-Range",
            "If-Unmodified-Since",
            "Max-Forwards",
            "Proxy-Authorization",
            "Referer",
            "Range",
            "Te",
            "Translate",
            "User-Agent"
        };

        private static Dictionary<string, int> _lookupTable = CreateLookupTable();

        private static Dictionary<string, int> CreateLookupTable()
        {
            Dictionary<string, int> lookupTable = new Dictionary<string, int>(_requestHeaderStrings.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _requestHeaderStrings.Length; i++)
            {
                lookupTable.Add(_requestHeaderStrings[i], i);
            }
            return lookupTable;
        }

        private static int KnownHeaderFromRequestHeader(string headerName)
        {
            int index;
            return _lookupTable.TryGetValue(headerName, out index) ? index : -1;
        }
    }
}
