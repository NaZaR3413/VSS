using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class Livestream
    {
        public string HlsUrl {  get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? GameScore
        {
            get
            {
                return $"{HomeScore} - {AwayScore}";
            }
        }
        public EventType? EventType { get; set; }
        public StreamStatus StreamStatus { get; set; }
    }
}
