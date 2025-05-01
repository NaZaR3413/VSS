using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace web_backend.Games
{
    public interface IGameAppService : IApplicationService
    {
        Task<GameDto> GetAsync(Guid id);
        
        Task<GameDto> CreateAsync(CreateUpdateGameDto input);
        
        Task<GameDto> UpdateAsync(Guid id, CreateUpdateGameDto input);
        
        Task DeleteAsync(Guid id);
        
        Task<List<GameDto>> GetListAsync();
        
        Task<List<GameDto>> GetFilteredListAsync(GameFilterDto input);
        
        // New methods for file uploads
        Task<GameDto> UploadGameAsync(GameUploadDto input);
        
        Task<GameDto> UpdateGameWithVideoAsync(Guid id, GameUploadDto input);
    }
}