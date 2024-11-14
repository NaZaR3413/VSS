using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Livestreams
{
    public interface ILivestreamRepository
    {
        Task<Livestream> GetAsync(Guid id);
        Task<List<Livestream>> GetListAsync();
        Task<Livestream> CreateAsync(Livestream livestream);
        Task<Livestream> UpdateAsync(Livestream livestream);

    }
}
