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
        /// <returns></returns>
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


    }
}
