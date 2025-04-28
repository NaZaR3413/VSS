using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace web_backend.Games
{
    public class GameAppService : ApplicationService, IGameAppService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IDataFilter _dataFilter;
        public GameAppService(
            IGameRepository gameRepository,
            IDataFilter dataFilter
            ) 
        { 
            _gameRepository = gameRepository;
            _dataFilter = dataFilter;
        }

        /// <summary>
        /// API to return a game instance
        /// </summary>
        /// <param name="id"></param>
        /// <returns>this specific game instance</returns>
        public async Task<GameDto> GetAsync(Guid id)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve game information
                var game = await _gameRepository.GetAsync(id);

                // map information onto dto
                var result = ObjectMapper.Map<Game, GameDto>(game);

                return result;

            }
        }

        /// <summary>
        /// API to return a list of all game instances
        /// </summary>
        /// <returns>list of all game instances</returns>
        public async Task<List<GameDto>> GetListAsync()
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve game list
                var game = await _gameRepository.GetListAsync();

                // map information onto a dto
                var result = ObjectMapper.Map<List<Game>, List<GameDto>>(game);

                return result;
            }
        }

        /// <summary>
        /// API to create a game instance
        /// </summary>
        /// <param name="input" of type CreateGameDto></param>
        /// <returns></returns>
        public async Task<GameDto> CreateAsync(CreateGameDto input)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var game = new Game
                {
                    GameUrl = input.GameUrl,
                    HomeTeam = input.HomeTeam,
                    AwayTeam = input.AwayTeam,
                    HomeScore = input.HomeScore,
                    AwayScore = input.AwayScore,
                    Broadcasters = input.Broadcasters,
                    Description = input.Description,
                    EventDate = input.EventDate,
                    EventType = input.EventType
                };

                var created = await _gameRepository.CreateAsync(game);
                return ObjectMapper.Map<Game, GameDto>(created);
            }
        }

        /// <summary>
        /// update a game instance 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"> updated information for the game</param>
        /// <returns>no specific return, updates the game instance</returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public async Task<GameDto> UpdateAsync(Guid id, UpdateGameDto input)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var game = await _gameRepository.GetAsync(id);
                if (game == null)
                {
                    throw new EntityNotFoundException(typeof(Game), id);
                }

                ObjectMapper.Map(input, game);

                var updatedGame = await _gameRepository.UpdateAsync(game);

                var result = ObjectMapper.Map<Game, GameDto>(updatedGame);

                return result;
            }
        }

        /// <summary>
        /// deletes a game instance 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public async Task DeleteAsync(Guid id)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var game = await _gameRepository.GetAsync(id);
                if (game == null)
                {
                    throw new EntityNotFoundException(typeof(Game), id);
                }
                await _gameRepository.DeleteAsync(id);
            }
        }

        /// <summary>
        /// Retrieves a list of games based on the provided filters.
        /// Supports filtering by EventType, HomeTeam, AwayTeam, Broadcasters, and EventDate.
        /// </summary>
        /// <param name="input">
        /// Filter parameters for querying games.
        /// - Visit <see cref="GameFilterDto"/> for details on available filters.
        /// - Dates should be provided in ISO 8601 format: YYYY-MM-DDTHH:MM:SS (e.g., 2025-05-01T19:30:00).
        /// </param>
        /// <returns>
        /// A list of matching <see cref="GameDto"/> objects.
        /// </returns>
        public async Task<List<GameDto>> GetFilteredListAsync(GameFilterDto input)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var filteredGames = await _gameRepository.GetFilteredListAsync(
                    input.EventType,
                    input.HomeTeam,
                    input.AwayTeam,
                    input.Broadcasters,
                    input.EventDate
                );

                var result = ObjectMapper.Map<List<Game>, List<GameDto>>(filteredGames);

                return result;
            }
        }



    }
}
