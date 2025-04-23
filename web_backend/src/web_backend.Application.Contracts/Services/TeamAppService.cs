using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using web_backend.Dtos;
using web_backend.Domain.Shared.Dtos;

namespace web_backend.HttpApi.Services
{
    public class TeamAppService : ITeamAppService
    {
        private readonly List<TeamDto> _teams = new();

        public Task<List<TeamDto>> GetListAsync()
        {
            return Task.FromResult(_teams);
        }

        public Task<TeamDto> CreateAsync(CreateTeamDto input)
        {
            var team = new TeamDto
            {
                Id = Guid.NewGuid(),
                Name = input.Name
            };
            _teams.Add(team);
            return Task.FromResult(team);
        }

        public Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto input)
        {
            var team = _teams.Find(t => t.Id == id);
            if (team != null)
            {
                team.Name = input.Name;
            }
            return Task.FromResult(team);
        }

        public Task DeleteAsync(Guid id)
        {
            var team = _teams.Find(t => t.Id == id);
            if (team != null)
            {
                _teams.Remove(team);
            }
            return Task.CompletedTask;
        }
    }
}
