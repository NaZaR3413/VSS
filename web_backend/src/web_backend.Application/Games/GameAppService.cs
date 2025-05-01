using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using web_backend.Services;

namespace web_backend.Games
{
    [Authorize]
    public class GameAppService : ApplicationService, IGameAppService
    {
        private readonly IRepository<Game, Guid> _gameRepository;
        private readonly IBlobStorageService _blobStorageService;
        private const string GameVideosContainer = "gamevideos";

        public GameAppService(
            IRepository<Game, Guid> gameRepository,
            IBlobStorageService blobStorageService)
        {
            _gameRepository = gameRepository;
            _blobStorageService = blobStorageService;
        }

        public async Task<GameDto> GetAsync(Guid id)
        {
            var game = await _gameRepository.GetAsync(id);
            return ObjectMapper.Map<Game, GameDto>(game);
        }

        [Authorize(Roles = "admin")]
        public async Task<GameDto> CreateAsync(CreateUpdateGameDto input)
        {
            var game = new Game(
                GuidGenerator.Create(),
                input.GameUrl ?? string.Empty,
                input.HomeTeam,
                input.AwayTeam,
                input.Description,
                input.EventDate,
                input.EventType
            )
            {
                HomeScore = input.HomeScore,
                AwayScore = input.AwayScore,
                Broadcasters = input.Broadcasters ?? string.Empty
            };

            await _gameRepository.InsertAsync(game);
            
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return ObjectMapper.Map<Game, GameDto>(game);
        }

        [Authorize(Roles = "admin")]
        public async Task<GameDto> UpdateAsync(Guid id, CreateUpdateGameDto input)
        {
            var game = await _gameRepository.GetAsync(id);

            game.GameUrl = input.GameUrl ?? string.Empty;
            game.HomeTeam = input.HomeTeam;
            game.AwayTeam = input.AwayTeam;
            game.HomeScore = input.HomeScore;
            game.AwayScore = input.AwayScore;
            game.Broadcasters = input.Broadcasters ?? game.Broadcasters;
            game.Description = input.Description;
            game.EventDate = input.EventDate;
            game.EventType = input.EventType;

            await _gameRepository.UpdateAsync(game);
            
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return ObjectMapper.Map<Game, GameDto>(game);
        }

        [Authorize(Roles = "admin")]
        public async Task DeleteAsync(Guid id)
        {
            var game = await _gameRepository.GetAsync(id);

            // If there's a video URL, attempt to delete it from blob storage
            if (!string.IsNullOrEmpty(game.GameUrl))
            {
                await _blobStorageService.DeleteFileAsync(game.GameUrl);
            }

            await _gameRepository.DeleteAsync(id);
            
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }
        }

        public async Task<List<GameDto>> GetListAsync()
        {
            var games = await _gameRepository.GetListAsync();
            return ObjectMapper.Map<List<Game>, List<GameDto>>(games);
        }

        public async Task<List<GameDto>> GetFilteredListAsync(GameFilterDto input)
        {
            var query = await _gameRepository.GetQueryableAsync();

            query = query.WhereIf(
                !string.IsNullOrEmpty(input.HomeTeam),
                g => g.HomeTeam.Contains(input.HomeTeam ?? string.Empty)
            );

            query = query.WhereIf(
                !string.IsNullOrEmpty(input.AwayTeam),
                g => g.AwayTeam.Contains(input.AwayTeam ?? string.Empty)
            );

            query = query.WhereIf(
                !string.IsNullOrEmpty(input.EventType),
                g => g.EventType.Contains(input.EventType ?? string.Empty)
            );

            query = query.WhereIf(
                input.FromDate.HasValue,
                g => g.EventDate >= input.FromDate!.Value
            );

            query = query.WhereIf(
                input.ToDate.HasValue,
                g => g.EventDate <= input.ToDate!.Value
            );

            var games = await query.ToListAsync();
            return ObjectMapper.Map<List<Game>, List<GameDto>>(games);
        }

        [Authorize(Roles = "admin")]
        public async Task<GameDto> UploadGameAsync(GameUploadDto input)
        {
            // Create a new game with the video URL
            var game = new Game(
                GuidGenerator.Create(),
                input.GameVideoUrl ?? string.Empty,
                input.HomeTeam,
                input.AwayTeam,
                input.Description,
                input.EventDate,
                input.EventType
            )
            {
                HomeScore = input.HomeScore,
                AwayScore = input.AwayScore,
                Broadcasters = input.Broadcasters ?? string.Empty
            };

            await _gameRepository.InsertAsync(game);
            
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return ObjectMapper.Map<Game, GameDto>(game);
        }

        [Authorize(Roles = "admin")]
        public async Task<GameDto> UpdateGameWithVideoAsync(Guid id, GameUploadDto input)
        {
            var game = await _gameRepository.GetAsync(id);

            // If a new video URL is provided, delete the old URL
            if (!string.IsNullOrEmpty(input.GameVideoUrl) && input.GameVideoUrl != game.GameUrl)
            {
                // Delete the old video if it exists
                if (!string.IsNullOrEmpty(game.GameUrl))
                {
                    await _blobStorageService.DeleteFileAsync(game.GameUrl);
                }

                // Update with the new video URL
                game.GameUrl = input.GameVideoUrl;
            }

            // Update other properties
            game.HomeTeam = input.HomeTeam;
            game.AwayTeam = input.AwayTeam;
            game.HomeScore = input.HomeScore;
            game.AwayScore = input.AwayScore;
            game.Broadcasters = input.Broadcasters ?? game.Broadcasters;
            game.Description = input.Description;
            game.EventDate = input.EventDate;
            game.EventType = input.EventType;

            await _gameRepository.UpdateAsync(game);
            
            if (CurrentUnitOfWork != null)
            {
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return ObjectMapper.Map<Game, GameDto>(game);
        }
    }
}