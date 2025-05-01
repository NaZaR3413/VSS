using System;
using System.ComponentModel.DataAnnotations;

namespace web_backend.Games
{
    public class GameDto
    {
        public Guid Id { get; set; }
        
        public string GameUrl { get; set; }
        
        public string HomeTeam { get; set; }
        
        public string AwayTeam { get; set; }
        
        public int? HomeScore { get; set; }
        
        public int? AwayScore { get; set; }
        
        public string Broadcasters { get; set; }
        
        public string Description { get; set; }
        
        public DateTime EventDate { get; set; }
        
        public string EventType { get; set; }
    }
}