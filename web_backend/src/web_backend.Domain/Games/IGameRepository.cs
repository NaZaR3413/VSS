using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Games
{
    public interface IGameRepository
    {
        Task<Game> GetAsync(Guid id);
        Task<List<Game>> GetListAsync();
        Task<Game> CreateAsync(Game game);
        Task<Game> UpdateAsync(Game game);
        Task DeleteAsync(Guid id);
        Task<List<Game>> GetFilteredListAsync(EventType? eventType, string? homeTeam, string? awayTeam, string? broadcasters, DateTime? eventDate);
    }
}
