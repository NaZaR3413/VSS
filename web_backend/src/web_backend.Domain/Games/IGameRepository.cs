using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Games
{
    public interface IGameRepository
    {
        Task<Game> GetAsync(Guid id);
        Task<List<Game>> GetListAsync();
        Task<Game> CreateAsync(Game game);
        Task<Game> UpdateAsync(Game game);
        Task DeleteAsync(Guid id);
    }
}
