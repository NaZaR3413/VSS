using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Games
{
    public class GameFilterDto
    {
        public EventType? EventType { get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public string? Broadcasters { get; set; }
        public DateTime? EventDate { get; set; }
    }
}
