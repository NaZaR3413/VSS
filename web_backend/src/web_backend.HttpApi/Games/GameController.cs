using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using web_backend.Services;

namespace web_backend.Games
{
    [Authorize]
    [RemoteService]
    [Route("api/app/game")]
    public class GameController : AbpController
    {
        private readonly IGameAppService _gameService;
        private readonly IBlobStorageService _blobStorageService;
        private const string GameVideosContainer = "gamevideos";
        
        public GameController(
            IGameAppService gameService, 
            IBlobStorageService blobStorageService)
        {
            _gameService = gameService;
            _blobStorageService = blobStorageService;
        }
        
        [HttpPost]
        [Authorize(Roles = "admin")]
        [Route("upload")]
        public async Task<GameDto> UploadGameAsync([FromForm] GameUploadModel model)
        {
            if (model.GameVideo == null || model.GameVideo.Length == 0)
            {
                throw new UserFriendlyException("No video file was uploaded");
            }
            
            if (model.GameVideo.Length > 500 * 1024 * 1024) // 500MB
            {
                throw new UserFriendlyException("The uploaded file exceeds the 500MB limit");
            }
            
            // Upload the file to Azure Blob Storage
            string gameVideoUrl;
            using (var stream = model.GameVideo.OpenReadStream())
            {
                gameVideoUrl = await _blobStorageService.UploadStreamAsync(
                    stream, 
                    model.GameVideo.FileName, 
                    model.GameVideo.ContentType, 
                    GameVideosContainer);
            }
            
            // Create the game with the uploaded video URL
            var gameDto = new GameUploadDto
            {
                GameVideoUrl = gameVideoUrl,
                HomeTeam = model.HomeTeam,
                AwayTeam = model.AwayTeam,
                HomeScore = model.HomeScore,
                AwayScore = model.AwayScore,
                Broadcasters = model.Broadcasters,
                Description = model.Description,
                EventDate = model.EventDate,
                EventType = model.EventType
            };
            
            return await _gameService.UploadGameAsync(gameDto);
        }
        
        [HttpPut]
        [Authorize(Roles = "admin")]
        [Route("upload/{id}")]
        public async Task<GameDto> UpdateGameWithVideoAsync(Guid id, [FromForm] GameUploadModel model)
        {
            // Initialize with empty URL if there's no file
            string? gameVideoUrl = string.Empty;
            
            // If a new video file was uploaded, process it
            if (model.GameVideo != null && model.GameVideo.Length > 0)
            {
                if (model.GameVideo.Length > 500 * 1024 * 1024) // 500MB
                {
                    throw new UserFriendlyException("The uploaded file exceeds the 500MB limit");
                }
                
                // Upload the file to Azure Blob Storage
                using (var stream = model.GameVideo.OpenReadStream())
                {
                    gameVideoUrl = await _blobStorageService.UploadStreamAsync(
                        stream, 
                        model.GameVideo.FileName, 
                        model.GameVideo.ContentType, 
                        GameVideosContainer);
                }
            }
            
            // Map to the DTO
            var gameDto = new GameUploadDto
            {
                GameVideoUrl = gameVideoUrl, // May be null if no file was uploaded
                HomeTeam = model.HomeTeam,
                AwayTeam = model.AwayTeam,
                HomeScore = model.HomeScore,
                AwayScore = model.AwayScore,
                Broadcasters = model.Broadcasters,
                Description = model.Description,
                EventDate = model.EventDate,
                EventType = model.EventType
            };
            
            return await _gameService.UpdateGameWithVideoAsync(id, gameDto);
        }
    }
    
    // This model is specifically for form uploads
    public class GameUploadModel
    {
        public IFormFile? GameVideo { get; set; }
        
        public required string HomeTeam { get; set; }
        
        public required string AwayTeam { get; set; }
        
        public int? HomeScore { get; set; }
        
        public int? AwayScore { get; set; }
        
        public string? Broadcasters { get; set; }
        
        public required string Description { get; set; }
        
        public DateTime EventDate { get; set; }
        
        public required string EventType { get; set; }
    }
}