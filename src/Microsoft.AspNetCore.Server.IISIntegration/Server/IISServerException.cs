// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISServerException : Win32Exception
    {
        internal IISServerException()
            : base(Marshal.GetLastWin32Error())
        {
        }
        internal IISServerException(int errorCode)
            : base(errorCode)
        {
        }

        internal IISServerException(int errorCode, string message)
            : base(errorCode, message)
        {
        }

        public override int ErrorCode => NativeErrorCode;
    }
}
