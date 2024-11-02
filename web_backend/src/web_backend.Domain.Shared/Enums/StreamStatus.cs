using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Enums
{
    public enum StreamStatus
    {
        [Description("Livestream is active and good to broadcast")]
        Active,
        [Description("Livestream is pending, something is still loading and it is not ready to broadcast")]
        Pending,
        [Description("Livestream has ended")]
        Complete
    }
}
