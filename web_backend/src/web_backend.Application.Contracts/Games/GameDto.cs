using System;
using Volo.Abp.Application.Dtos;
using web_backend.Enums;

namespace web_backend.Games
{
    public class GameDto : EntityDto<Guid>
    {
        public Guid Id { get; set; }
        public string PlaybackUrl { get; set; }
        public string GameUrl { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string Broadcasters { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public EventType EventType { get; set; }

        // Computed properties for convenience 
        public string GameScore
        {
            get
            {
                return $"{HomeScore} - {AwayScore}";
            }
        }

        public string DisplayName
        {
            get
            {
                return $"{HomeTeam} vs. {AwayTeam}";
            }
        }
    }
}
