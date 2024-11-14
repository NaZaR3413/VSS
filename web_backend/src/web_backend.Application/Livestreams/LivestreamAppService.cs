using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace web_backend.Livestreams
{
    public class LivestreamAppService : ApplicationService, ILivestreamAppService
    {
        private readonly ILivestreamRepository _livestreamRepository;
        public LivestreamAppService(
            ILivestreamRepository livestreamRepository
            )
        {
            _livestreamRepository = livestreamRepository;
        }

        // return specific instance of a livestream, if it exists
        public async Task<LivestreamDto> GetAsync(Guid id)
        {
            // retrieve livestream information
            var livestream = await _livestreamRepository.GetAsync(id);

            // map livestream info onto Dto
            var result = ObjectMapper.Map<Livestream, LivestreamDto>(livestream);

            return result;

        }

        // return a list of every livestream table instance, regardless of status
        public async Task<List<LivestreamDto>> GetListAsync()
        {
            var livestreams = await _livestreamRepository.GetListAsync();
            var result = ObjectMapper.Map<List<Livestream>, List<LivestreamDto>>(livestreams);
            return result;
        }
    }
}
