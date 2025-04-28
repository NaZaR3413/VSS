using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
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


    }
}
