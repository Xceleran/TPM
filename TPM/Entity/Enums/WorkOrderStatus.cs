using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSM.Entity.Enums
{
    public enum WorkOrderStatus
    {
        Pending = 1,
        Scheduled = 2,
        Cancelled = 3,
        Closed = 4,
        InstallationInProgress = 5
    }
}
