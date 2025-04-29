using System;
using web_backend.Enums;

public class LivestreamDto
{
    public Guid Id { get; set; }
    public string HlsUrl { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }
    public int HomeScore { get; set; } = 0;
    public int AwayScore { get; set; } = 0;
    public bool FreeLivestream { get; set; } = false;
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
    public EventType EventType { get; set; }
    public StreamStatus StreamStatus { get; set; }
    public DateTime EventDate { get; set; }
}
