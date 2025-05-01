using System;

namespace web_backend.Games
{
    public class GameFilterDto
    {
        public string HomeTeam { get; set; }
        
        public string AwayTeam { get; set; }
        
        public DateTime? FromDate { get; set; }
        
        public DateTime? ToDate { get; set; }
        
        public string EventType { get; set; }
    }
}