using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using web_backend.Enums;

namespace web_backend.Games
{
    public class Game : Entity<Guid>
    {
        public string GameUrl { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
        public int HomeScore { get; set; } = 0;
        public int AwayScore { get; set; } = 0;
        public string Broadcasters { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public EventType EventType { get; set; }
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
