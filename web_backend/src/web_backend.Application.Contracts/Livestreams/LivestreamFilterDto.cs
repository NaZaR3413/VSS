using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class LivestreamFilterDto
    {
        // All set to nullable in order to allow for proper filtering
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public EventType? EventType { get; set; }
        public StreamStatus? StreamStatus { get; set; }
    }
}
