using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Games
{
    public class CreateGameDto
    {
        public string? GameUrl { get; set; }          // ⇐ nullable (server will fill)
        public string HomeTeam { get; set; } = default!;
        public string AwayTeam { get; set; } = default!;
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string? Broadcasters { get; set; }
        public string Description { get; set; } = default!;
        public DateTime EventDate { get; set; }
        public EventType EventType { get; set; }

        public IFormFile VideoFile { get; set; } = default!;   // ← keep this exact name
    }

}
