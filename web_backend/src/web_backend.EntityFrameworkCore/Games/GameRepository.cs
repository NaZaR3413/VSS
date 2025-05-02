using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using web_backend.EntityFrameworkCore;
using web_backend.Enums;

namespace web_backend.Games
{
    public class GameRepository : EfCoreRepository<web_backendDbContext, Game, Guid>, IGameRepository
    {
        public GameRepository(IDbContextProvider<web_backendDbContext> dbContextProvider)
           : base(dbContextProvider)
        {
        }

        public async Task<Game> GetAsync(Guid id)
        {
            var query = await GetQueryableAsync();
            var result = await query.FirstOrDefaultAsync(x => x.Id == id);

            return result;
        }

        public async Task<List<Game>> GetListAsync()
        {
            var query = await GetQueryableAsync();
            return await query.ToListAsync();
        }

        public async Task<Game> CreateAsync(Game game)
        {
            return await InsertAsync(game);
        }

        public async Task<Game> UpdateAsync(Game game)
        {
            var updatedGame = await base.UpdateAsync(game);
            return updatedGame;
        }

        public async Task DeleteAsync(Guid id)
        {
            await base.DeleteAsync(id);
        }

        public async Task<List<Game>> GetFilteredListAsync(EventType? eventType, string? homeTeam, string? awayTeam, string? broadcasters, DateTime? eventDate)
        {
            var query = await GetQueryableAsync();
            
            query = new List<Func<IQueryable<Game>, IQueryable<Game>>>
                {
                    q => eventType.HasValue ? q.Where(g => g.EventType == eventType) : q,
                    q => !string.IsNullOrWhiteSpace(homeTeam) ? q.Where(g => g.HomeTeam.Contains(homeTeam)) : q,
                    q => !string.IsNullOrWhiteSpace(awayTeam) ? q.Where(g => g.AwayTeam.Contains(awayTeam)) : q,
                    q => !string.IsNullOrWhiteSpace(broadcasters) ? q.Where(g => g.Broadcasters.Contains(broadcasters)) : q,
                    q => eventDate.HasValue ? q.Where(g => g.EventDate.Date == eventDate.Value.Date) : q
                }.Aggregate(query, (current, filter) => filter(current));
            
            return await query.ToListAsync();
        }

    }
}
