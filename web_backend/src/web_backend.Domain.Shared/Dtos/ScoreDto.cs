using System;

namespace web_backend.Domain.Shared.Dtos
{
    public class ScoreDto
    {
        public Guid Id { get; set; }
        public string TeamA { get; set; }
        public string TeamB { get; set; }
        public int ScoreA { get; set; }
        public int ScoreB { get; set; }
    }
}
