using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Enums;

namespace web_backend.Games
{
    public interface IGameRepository
    {
        /* CRUD */
        Task<Game> GetAsync(Guid id);
        Task<List<Game>> GetListAsync();
        Task<Game> CreateAsync(Game game);
        Task<Game> UpdateAsync(Game game);
        Task DeleteAsync(Guid id);

        /* Filtering (primitive parameters only) */
        Task<List<Game>> GetFilteredListAsync(
            EventType? eventType,
            string? homeTeam,
            string? awayTeam,
            string? broadcasters,
            DateTime? eventDate);
    }
}
