using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace web_backend.Games
{
    public interface IGameAppService : IApplicationService
    {
        Task<GameDto> GetAsync(Guid id);
        Task<List<GameDto>> GetListAsync();
        Task<GameDto> CreateAsync(CreateGameDto input);
    }
}
