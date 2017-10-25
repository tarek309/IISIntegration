using System;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISDelegates
    {
        internal delegate REQUEST_NOTIFICATION_STATUS PFN_REQUEST_HANDLER(IntPtr pHttpContext, IntPtr pvRequestContext);
        internal delegate bool PFN_SHUTDOWN_HANDLER(IntPtr pvRequestContext);
        internal delegate REQUEST_NOTIFICATION_STATUS PFN_ASYNC_COMPLETION(IntPtr pvManagedHttpContext, int hr, int bytes);
        internal delegate REQUEST_NOTIFICATION_STATUS PFN_WEBSOCKET_ASYNC_COMPLETION(IntPtr pHttpContext, IntPtr completionInfo, IntPtr pvCompletionContext);
    }
}
