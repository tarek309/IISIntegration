using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tests
{
    internal class TestIISContext
    {
        internal HttpApiTypes.HTTP_REQUEST_V2 NativeRequest { get; set; }
        internal IntPtr ManagedServerPointer { get; set; }
        internal List<GCHandle> GCHandles { get; set; }
        internal HttpContext Context { get; set; }
        internal HttpApiTypes.HTTP_RESPONSE_V2 NativeResponse { get; set; }
        internal REQUEST_NOTIFICATION_STATUS RequestNotificationStatus { get; set; }
        internal bool Shutdown { get; set; }
        internal bool WebsocketsEnabled { get; set; }
        internal bool PostCompletionCalled { get; set; }
        internal CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        internal Task CompleteRequest { get; set; }
    }
}
