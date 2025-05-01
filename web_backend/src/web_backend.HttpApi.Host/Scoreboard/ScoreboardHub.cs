using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace web_backend.Scoreboard
{
    public class ScoreboardHub : Hub
    {
        public async Task UpdateScore(string gameId, int homeScore, int awayScore)
        {
            await Clients.Others.SendAsync("UpdateScore", gameId, homeScore, awayScore);
        }

        public async Task RefreshScoreboard(string gameId)
        {
            await Clients.Others.SendAsync("RefreshScoreboard", gameId);
        }
        
        public async Task UpdateScoreboardState(string scoreboardId, string gameStateJson)
        {
            await Clients.Others.SendAsync("UpdateScoreboardState", scoreboardId, gameStateJson);
        }
    }
}