using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Games
{
    public class UpdateGameDto
    {
        public string? GameUrl { get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? Broadcasters { get; set; }
        public string? Description { get; set; }
        public DateTime? EventDate { get; set; }
        public EventType? EventType { get; set; }
    }
}
