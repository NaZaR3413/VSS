using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
