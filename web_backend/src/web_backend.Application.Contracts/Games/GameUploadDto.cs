using System;
using System.ComponentModel.DataAnnotations;

namespace web_backend.Games
{
    public class GameUploadDto
    {
        // We'll use a string URL instead of IFormFile to avoid reference issues
        public string? GameVideoUrl { get; set; }
        
        [Required]
        public required string HomeTeam { get; set; }
        
        [Required]
        public required string AwayTeam { get; set; }
        
        public int? HomeScore { get; set; }
        
        public int? AwayScore { get; set; }
        
        public string? Broadcasters { get; set; }
        
        [Required]
        public required string Description { get; set; }
        
        [Required]
        public DateTime EventDate { get; set; }
        
        [Required]
        public required string EventType { get; set; }
    }
}