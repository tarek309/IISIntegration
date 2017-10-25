using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    enum REQUEST_NOTIFICATION_STATUS
    {
        RQ_NOTIFICATION_CONTINUE,
        RQ_NOTIFICATION_PENDING,
        RQ_NOTIFICATION_FINISH_REQUEST
    }
}
