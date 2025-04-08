using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class Livestream : Entity<Guid>
    {
        public string HlsUrl {  get; set; }
        public Guid HomeTeamId { get; set; }
        public Guid AwayTeamId { get; set; }
        public int HomeScore { get; set; } = 0;
        public int AwayScore { get; set; } = 0;
        public string GameScore
        {
            get
            {
                return $"{HomeScore} - {AwayScore}";
            }
        }
        public EventType EventType { get; set; }
        public StreamStatus StreamStatus { get; set; }

        public DateTime EventDate { get; set; }
    }
}
