using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Domain.Shared.Dtos;
using web_backend.Dtos;
using web_backend.HttpApi.Services;

namespace web_backend.HttpApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamAppService _teamAppService;

        public TeamsController(ITeamAppService teamAppService)
        {
            _teamAppService = teamAppService;
        }

        [HttpGet]
        public async Task<List<TeamDto>> GetTeams()
        {
            return await _teamAppService.GetListAsync();
        }

        [HttpPost]
        public async Task<TeamDto> CreateTeam(CreateTeamDto input)
        {
            return await _teamAppService.CreateAsync(input);
        }

        [HttpPut("{id}")]
        public async Task<TeamDto> UpdateTeam(Guid id, UpdateTeamDto input)
        {
            return await _teamAppService.UpdateAsync(id, input);
        }

        [HttpDelete("{id}")]
        public async Task DeleteTeam(Guid id)
        {
            await _teamAppService.DeleteAsync(id);
        }
    }
}
