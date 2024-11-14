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
    }
}
