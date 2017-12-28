using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCoreModule.FunctionalTests
{
    [Flags]
    public enum ANCMFlags
    {
        None,
        UseIISExpress,
        UseFullIIS,
        MakeCertExeAvailable,
        WebSocketModuleAvailable,
        UrlRewriteModuleAvailable
    }
}
