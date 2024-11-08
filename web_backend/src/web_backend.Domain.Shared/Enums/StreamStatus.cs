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
        [Description("User has setup a game and received a stream key, waiting on user to access RTMP server with this stream key")]
        AwaitingConnection,
        [Description("User is using instance specific stream key, pending 'start' of livestream")]
        PendingStart,
        [Description("Livestream is active and good to broadcast")]
        Live,
        [Description("Livestream has ended, remove from active streams")]
        Completed
    }
}
