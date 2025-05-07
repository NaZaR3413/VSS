using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace web_backend.Games;

[RemoteService]
[Area("app")]
[Route("api/app/game")]
public class GameAppService : ApplicationService, IGameAppService
{
    private readonly IGameRepository _repo;
    private readonly IObjectMapper _mapper;

    public GameAppService(IGameRepository repo, IObjectMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    /* ----------  Queries  ---------- */

    [HttpGet("{id}")]
    public async Task<GameDto> GetAsync(Guid id) =>
        _mapper.Map<Game, GameDto>(await _repo.GetAsync(id));

    [HttpGet]
    public async Task<List<GameDto>> GetListAsync() =>
        _mapper.Map<List<Game>, List<GameDto>>(await _repo.GetListAsync());

    [HttpPost("filter")]
    public async Task<List<GameDto>> GetFilteredListAsync([FromBody] GameFilterDto f)
    {
        var list = await _repo.GetFilteredListAsync(
            f.EventType, f.HomeTeam, f.AwayTeam, f.Broadcasters, f.EventDate);

        return _mapper.Map<List<Game>, List<GameDto>>(list);
    }

    /* ----------  Commands  ---------- */

    // POST /api/app/game/upload   (multipart/form‑data)
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<GameDto> CreateAsync([FromForm] CreateGameDto input)
    {
        string videoRel = string.Empty;

        if (input.VideoFile is { Length: > 0 })
        {
            var root = Path.Combine(AppContext.BaseDirectory, "wwwroot", "videos");
            Directory.CreateDirectory(root);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(input.VideoFile.FileName)}";
            var fullPath = Path.Combine(root, fileName);

            await using var fs = new FileStream(fullPath, FileMode.Create);
            await input.VideoFile.CopyToAsync(fs);

            videoRel = $"/videos/{fileName}";
        }

        var entity = _mapper.Map<CreateGameDto, Game>(input);
        entity.GameUrl = videoRel;
        entity.PlaybackUrl = videoRel;

        entity = await _repo.CreateAsync(entity);    // autoSave true in repository
        return _mapper.Map<Game, GameDto>(entity);
    }

    // PUT /api/app/game/{id}
    [HttpPut("{id}")]
    public async Task<GameDto> UpdateAsync(Guid id, [FromBody] UpdateGameDto input)
    {
        var entity = await _repo.GetAsync(id);
        _mapper.Map(input, entity);

        entity = await _repo.UpdateAsync(entity);
        return _mapper.Map<Game, GameDto>(entity);
    }

    // DELETE /api/app/game/{id}
    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
