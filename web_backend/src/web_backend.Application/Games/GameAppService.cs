using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;
using web_backend.Games;

namespace web_backend.Games;

[RemoteService]                       // enables auto‑proxy for Blazor WASM
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
    public async Task<GameDto> GetAsync(Guid id)
        => _mapper.Map<Game, GameDto>(await _repo.GetAsync(id));

    [HttpGet]
    public async Task<List<GameDto>> GetListAsync()
        => _mapper.Map<List<Game>, List<GameDto>>(await _repo.GetListAsync());

    [HttpPost("filter")]
    public async Task<List<GameDto>> GetFilteredListAsync([FromBody] GameFilterDto input)
    {
        var list = await _repo.GetFilteredListAsync(
            input.EventType, input.HomeTeam, input.AwayTeam,
            input.Broadcasters, input.EventDate);

        return _mapper.Map<List<Game>, List<GameDto>>(list);
    }

    /* ----------  Commands  ---------- */

    // multipart/form‑data  POST  /api/app/game/upload
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<GameDto> CreateAsync([FromForm] CreateGameDto input)
    {
        // simple file save to local wwwroot/videos – adapt for blob storage as needed
        var videoPath = string.Empty;
        if (input.VideoFile is { Length: > 0 })
        {
            var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "videos");
            Directory.CreateDirectory(webRoot);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(input.VideoFile.FileName)}";
            var fullPath = Path.Combine(webRoot, fileName);

            await using var fs = new FileStream(fullPath, FileMode.Create);
            await input.VideoFile.CopyToAsync(fs);

            videoPath = $"/videos/{fileName}";
        }

        var entity = _mapper.Map<CreateGameDto, Game>(input);
        entity.GameUrl = videoPath;
        entity.PlaybackUrl = videoPath;

        entity = await _repo.CreateAsync(entity);
        return _mapper.Map<Game, GameDto>(entity);
    }

    // PUT  /api/app/game/{id}
    [HttpPut("{id}")]
    public async Task<GameDto> UpdateAsync(Guid id, [FromBody] UpdateGameDto input)
    {
        var entity = await _repo.GetAsync(id);
        _mapper.Map(input, entity);

        entity = await _repo.UpdateAsync(entity);
        return _mapper.Map<Game, GameDto>(entity);
    }

    // DELETE  /api/app/game/{id}
    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
