using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using web_backend.EntityFrameworkCore;
using web_backend.Enums;
using web_backend.Games;

namespace web_backend.Games
{
    public class GameRepository
        : EfCoreRepository<web_backendDbContext, Game, Guid>,
          IGameRepository
    {
        public GameRepository(IDbContextProvider<web_backendDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        /* ----------  CRUD  ---------- */

        public async Task<Game> GetAsync(Guid id)
        {
            var qry = await GetQueryableAsync();
            return await qry.FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<List<Game>> GetListAsync()
        {
            var qry = await GetQueryableAsync();
            return await qry.ToListAsync();
        }

        public async Task<Game> CreateAsync(Game game)
    => await InsertAsync(game, autoSave: true);          // force SaveChanges

        public async Task<Game> UpdateAsync(Game game)
            => await base.UpdateAsync(game, autoSave: true);     // idem


        public async Task DeleteAsync(Guid id) => await base.DeleteAsync(id);

        /* ----------  Filtering helpers  ---------- */

        private static IQueryable<Game> ApplyFilters(
            IQueryable<Game> qry,
            EventType? eventType,
            string? homeTeam,
            string? awayTeam,
            string? broadcasters,
            DateTime? eventDate)
        {
            if (eventType.HasValue)
                qry = qry.Where(g => g.EventType == eventType);

            if (!string.IsNullOrWhiteSpace(homeTeam))
                qry = qry.Where(g => g.HomeTeam.Contains(homeTeam));

            if (!string.IsNullOrWhiteSpace(awayTeam))
                qry = qry.Where(g => g.AwayTeam.Contains(awayTeam));

            if (!string.IsNullOrWhiteSpace(broadcasters))
                qry = qry.Where(g => g.Broadcasters.Contains(broadcasters));

            if (eventDate.HasValue)
                qry = qry.Where(g => g.EventDate.Date == eventDate.Value.Date);

            return qry;
        }

        /* original signature (still used elsewhere) */
        public async Task<List<Game>> GetFilteredListAsync(
            EventType? eventType,
            string? homeTeam,
            string? awayTeam,
            string? broadcasters,
            DateTime? eventDate)
        {
            var qry = await GetQueryableAsync();
            qry = ApplyFilters(qry, eventType, homeTeam, awayTeam, broadcasters, eventDate);
            return await qry.ToListAsync();
        }

    }
}
