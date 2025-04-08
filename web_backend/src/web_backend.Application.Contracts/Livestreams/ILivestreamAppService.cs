using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace web_backend.Livestreams
{
    public interface ILivestreamAppService : IApplicationService
    {
        Task<LivestreamDto> GetAsync(Guid id);
        Task<List<LivestreamDto>> GetListAsync();
        Task<LivestreamDto> CreateAsync(CreateLivestreamDto input);
        Task<LivestreamDto> UpdateAsync(Guid id, UpdateLivestreamDto input);
        Task DeleteAsync(Guid id);
    }
}
