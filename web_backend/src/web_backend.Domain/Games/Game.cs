using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace web_backend.Games
{
    public class Game : AuditedAggregateRoot<Guid>
    {
        public string GameUrl { get; set; }
        
        public string HomeTeam { get; set; }
        
        public string AwayTeam { get; set; }
        
        public int? HomeScore { get; set; }
        
        public int? AwayScore { get; set; }
        
        public string Broadcasters { get; set; }
        
        public string Description { get; set; }
        
        public DateTime EventDate { get; set; }
        
        public string EventType { get; set; }
        
        // Default constructor required by EF Core
        protected Game() { }
        
        public Game(
            Guid id,
            string gameUrl,
            string homeTeam,
            string awayTeam,
            string description,
            DateTime eventDate,
            string eventType)
            : base(id)
        {
            GameUrl = gameUrl;
            HomeTeam = homeTeam;
            AwayTeam = awayTeam;
            Description = description;
            EventDate = eventDate;
            EventType = eventType;
        }
    }
}