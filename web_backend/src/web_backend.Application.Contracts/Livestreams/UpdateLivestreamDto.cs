using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class UpdateLivestreamDto
    {
        // All properties are nullable in order to allow partial updates
        public string? HlsUrl { get; set; } 
        public string? HomeTeam { get; set; } 
        public string? AwayTeam { get; set; }  
        public int? HomeScore { get; set; }  
        public int? AwayScore { get; set; }  
        public EventType? EventType { get; set; } 
        public StreamStatus? StreamStatus { get; set; } 
    }
}
