using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Dtos;
using web_backend.Domain.Shared.Dtos;

namespace web_backend.HttpApi.Services
{
    public interface ITeamAppService
    {
        Task<List<TeamDto>> GetListAsync();
        Task<TeamDto> CreateAsync(CreateTeamDto input);
        Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto input);
        Task DeleteAsync(Guid id);
    }
}
