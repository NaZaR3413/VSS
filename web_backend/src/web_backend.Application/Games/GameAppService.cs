using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.ObjectMapping;
using Azure.Storage.Blobs;                     // ← Azure SDK

namespace web_backend.Games;

[RemoteService]
[Area("app")]
[Route("api/app/game")]
public class GameAppService : ApplicationService, IGameAppService
{
    private const string ContainerName = "videos";

    private readonly IGameRepository _repo;
    private readonly IObjectMapper _map;
    private readonly IBlobContainer _blob;          // default container
    private readonly BlobServiceClient _blobSvc;      // Azure url helper

    public GameAppService(
        IGameRepository repo,
        IObjectMapper map,
        IBlobContainer blob,
        BlobServiceClient blobSvc)                    // injected by AbpBlobStoring.Azure
    {
        _repo = repo;
        _map = map;
        _blob = blob;
        _blobSvc = blobSvc;
    }

    /* ----------  Queries ---------- */

    [HttpGet("{id}")]
    public async Task<GameDto> GetAsync(Guid id) =>
        _map.Map<Game, GameDto>(await _repo.GetAsync(id));

    [HttpGet]
    public async Task<List<GameDto>> GetListAsync() =>
        _map.Map<List<Game>, List<GameDto>>(await _repo.GetListAsync());

    [HttpPost("filter")]
    public async Task<List<GameDto>> GetFilteredListAsync([FromBody] GameFilterDto f)
    {
        var list = await _repo.GetFilteredListAsync(
            f.EventType, f.HomeTeam, f.AwayTeam, f.Broadcasters, f.EventDate);

        return _map.Map<List<Game>, List<GameDto>>(list);
    }

    /* ----------  Commands ---------- */

    // POST /api/app/game/upload (multipart/form‑data)
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<GameDto> CreateAsync([FromForm] CreateGameDto input)
    {
        string blobName = string.Empty;
        string publicUrl = string.Empty;

        if (input.VideoFile is { Length: > 0 })
        {
            blobName = $"{Guid.NewGuid()}{Path.GetExtension(input.VideoFile.FileName)}";

            await using var stream = input.VideoFile.OpenReadStream();
            await _blob.SaveAsync(blobName, stream, overrideExisting: false);

            // build https://{account}.blob.core.windows.net/videos/{blobName}
            var container = _blobSvc.GetBlobContainerClient(ContainerName);
            publicUrl = container.GetBlobClient(blobName).Uri.ToString();
        }

        var entity = _map.Map<CreateGameDto, Game>(input);
        entity.GameUrl = publicUrl;
        entity.PlaybackUrl = publicUrl;

        entity = await _repo.CreateAsync(entity);
        return _map.Map<Game, GameDto>(entity);
    }

    [HttpPut("{id}")]
    public async Task<GameDto> UpdateAsync(Guid id, [FromBody] UpdateGameDto input)
    {
        var e = await _repo.GetAsync(id);
        _map.Map(input, e);

        e = await _repo.UpdateAsync(e);
        return _map.Map<Game, GameDto>(e);
    }

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
