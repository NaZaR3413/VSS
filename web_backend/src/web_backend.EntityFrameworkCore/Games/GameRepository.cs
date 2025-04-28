using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using web_backend.EntityFrameworkCore;

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

        }
    }
}
