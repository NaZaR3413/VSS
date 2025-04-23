using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class CreateLivestreamDto
    {
        public string HlsUrl { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public EventType EventType { get; set; }
        public StreamStatus StreamStatus { get; set; }
        public DateTime EventDate { get; set; }
        public string HomeTeam { get; set; }
        public string AwayTeam { get; set; }
    }


}
