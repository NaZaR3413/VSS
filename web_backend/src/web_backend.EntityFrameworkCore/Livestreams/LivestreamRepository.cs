using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using web_backend.EntityFrameworkCore;
using web_backend.Enums;

namespace web_backend.Livestreams
{
    public class LivestreamRepository : EfCoreRepository<web_backendDbContext, Livestream, Guid>, ILivestreamRepository
    {
        public LivestreamRepository(IDbContextProvider<web_backendDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<Livestream> GetAsync(Guid id)
        {
            var query = await GetQueryableAsync();
            var result = await query.FirstOrDefaultAsync(x => x.Id == id);

            return result;
        }

        public async Task<List<Livestream>> GetListAsync()
        {
            var query = await GetQueryableAsync();
            return await query.ToListAsync();
        }

        public async Task<Livestream> CreateAsync(Livestream livestream)
        {
            return await InsertAsync(livestream);
        }

        public async Task<Livestream> UpdateAsync(Livestream livestream)
        {
            var updatedLivestream = await base.UpdateAsync(livestream);

            return updatedLivestream;
        }

        public async Task<List<Livestream>> GetFilteredListAsync(EventType? eventType, StreamStatus? streamStatus, string? homeTeam, string? awayTeam)
        {
            var query = await GetQueryableAsync();

            // Create a list of filter functions based on individual filter parameters
            query = new List<Func<IQueryable<Livestream>, IQueryable<Livestream>>>
            {
                q => eventType.HasValue ? q.Where(livestream => livestream.EventType == eventType) : q,
                q => streamStatus.HasValue ? q.Where(livestream => livestream.StreamStatus == streamStatus) : q,
                q => !string.IsNullOrWhiteSpace(homeTeam) ? q.Where(livestream => livestream.HomeTeam.Contains(homeTeam)) : q,
                q => !string.IsNullOrWhiteSpace(awayTeam) ? q.Where(livestream => livestream.AwayTeam.Contains(awayTeam)) : q
            }.Aggregate(query, (current, filter) => filter(current));

            return await query.ToListAsync();
        }
        public async Task DeleteAsync(Guid id)
        {
            await base.DeleteAsync(id);
        }
    }
}
