using System;
using System.ComponentModel.DataAnnotations;

namespace web_backend.Games
{
    public class CreateUpdateGameDto
    {
        public string GameUrl { get; set; }
        
        [Required]
        public string HomeTeam { get; set; }
        
        [Required]
        public string AwayTeam { get; set; }
        
        public int? HomeScore { get; set; }
        
        public int? AwayScore { get; set; }
        
        public string Broadcasters { get; set; }
        
        [Required]
        public string Description { get; set; }
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Required]
        public string EventType { get; set; }
    }
}