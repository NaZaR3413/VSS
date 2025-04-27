using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using web_backend.Controllers;
using web_backend.Enums;

namespace web_backend.Livestreams;

[RemoteService]
[EnableCors("AllowAllOrigins")] // Add the more permissive CORS policy
[Route("api/app/livestream")]
public class LivestreamController : web_backendController
{
    private readonly ILivestreamAppService _livestreamAppService;

    public LivestreamController(ILivestreamAppService livestreamAppService)
    {
        _livestreamAppService = livestreamAppService;
    }

    [HttpGet]
    public Task<List<LivestreamDto>> GetListAsync()
    {
        try 
        {
            return _livestreamAppService.GetListAsync();
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error in GetListAsync: {ex.Message}");
            throw;
        }
    }

    [HttpGet]
    [Route("{id}")]
    public Task<LivestreamDto> GetAsync(Guid id)
    {
        return _livestreamAppService.GetAsync(id);
    }

    [HttpPost]
    public Task<LivestreamDto> CreateAsync(CreateLivestreamDto input)
    {
        return _livestreamAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public Task<LivestreamDto> UpdateAsync(Guid id, UpdateLivestreamDto input)
    {
        return _livestreamAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public Task DeleteAsync(Guid id)
    {
        return _livestreamAppService.DeleteAsync(id);
    }

    [HttpPost]
    [Route("{id}/end")]
    public async Task<LivestreamDto> EndStreamAsync(Guid id)
    {
        // Get the current stream
        var stream = await _livestreamAppService.GetAsync(id);
        
        // Create an update DTO with the completed status
        var updateDto = new UpdateLivestreamDto
        {
            HomeTeam = stream.HomeTeam,
            AwayTeam = stream.AwayTeam,
            HlsUrl = stream.HlsUrl,
            HomeScore = stream.HomeScore,
            AwayScore = stream.AwayScore,
            EventType = stream.EventType,
            StreamStatus = StreamStatus.Completed // Use Completed instead of Ended
        };
        
        return await _livestreamAppService.UpdateAsync(id, updateDto);
    }

    [HttpPost]
    [Route("{id}/set-status")]
    public async Task<LivestreamDto> SetStreamStatusAsync(Guid id, [FromBody] StreamStatusDto statusDto)
    {
        // Get the current stream
        var stream = await _livestreamAppService.GetAsync(id);
        
        // Create an update DTO with the new status
        var updateDto = new UpdateLivestreamDto
        {
            HomeTeam = stream.HomeTeam,
            AwayTeam = stream.AwayTeam,
            HlsUrl = stream.HlsUrl,
            HomeScore = stream.HomeScore,
            AwayScore = stream.AwayScore,
            EventType = stream.EventType,
            StreamStatus = statusDto.Status
        };
        
        return await _livestreamAppService.UpdateAsync(id, updateDto);
    }

    [HttpGet]
    [Route("by-status/{status}")]
    public async Task<List<LivestreamDto>> GetByStatusAsync(StreamStatus status)
    {
        var allStreams = await _livestreamAppService.GetListAsync();
        return allStreams.Where(s => s.StreamStatus == status).ToList();
    }
}