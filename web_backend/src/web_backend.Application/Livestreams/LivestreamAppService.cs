using AutoMapper.Internal.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace web_backend.Livestreams
{
    public class LivestreamAppService : ApplicationService, ILivestreamAppService
    {
        private readonly ILivestreamRepository _livestreamRepository;
        private readonly IDataFilter _dataFilter;
        public LivestreamAppService(
            ILivestreamRepository livestreamRepository,
            IDataFilter dataFilter
            )
        {
            _livestreamRepository = livestreamRepository;
            _dataFilter = dataFilter;
        }

        // return specific instance of a livestream, if it exists
        public async Task<LivestreamDto> GetAsync(Guid id)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve livestream information
                var livestream = await _livestreamRepository.GetAsync(id);

                // map livestream info onto Dto
                var result = ObjectMapper.Map<Livestream, LivestreamDto>(livestream);

                return result;
            }
        }

        // return a list of every livestream table instance, regardless of status
        public async Task<List<LivestreamDto>> GetListAsync()
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve a list of all livestreams
                var livestreams = await _livestreamRepository.GetListAsync();

                // map livestream list onto dto and return
                var result = ObjectMapper.Map<List<Livestream>, List<LivestreamDto>>(livestreams);
                return result;
            }
        }

        // create livestream instance 
        public async Task<LivestreamDto> CreateAsync(CreateLivestreamDto input)
        {
            // map input dto onto livestream 
            var livestream = ObjectMapper.Map<CreateLivestreamDto, Livestream>(input);

            // create livestream instance
            var createdLivestream = await _livestreamRepository.CreateAsync(livestream);

            // map new entity onto livestreamDto and return
            var result = ObjectMapper.Map<Livestream, LivestreamDto>(createdLivestream);

            return result;
        }

        // update livestream instance
        public async Task<LivestreamDto> UpdateAsync(Guid id, UpdateLivestreamDto input)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve existing livestream
                var livestream = await _livestreamRepository.GetAsync(id);
                if (livestream == null)
                {
                    throw new EntityNotFoundException(typeof(Livestream), id);
                }

                // map input dto onto existing livestream entity
                ObjectMapper.Map(input, livestream);

                // update livestream using _repository
                var updatedLivestream = await _livestreamRepository.UpdateAsync(livestream);

                // map updated entity onto livestreamDto and return
                var result = ObjectMapper.Map<Livestream, LivestreamDto>(updatedLivestream);

                return result;
            }
        }

        // return a list of filtered livestreams
        public async Task<List<LivestreamDto>> GetFilteredListAsync(LivestreamFilterDto input)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // retrieve list of livestreams with the applied filters
                var filteredLivestreams = await _livestreamRepository.GetFilteredListAsync(
                input.EventType,
                input.StreamStatus,
                input.HomeTeam,
                input.AwayTeam
             );

                // map the resulting list and return
                var result = ObjectMapper.Map<List<Livestream>, List<LivestreamDto>>(filteredLivestreams);

                return result;
            }

        }
        public async Task DeleteAsync(Guid id)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                // verify existance
                var livestream = await _livestreamRepository.GetAsync(id);
                if (livestream == null)
                {
                    throw new EntityNotFoundException(typeof(Livestream), id);
                }

                // Delete the livestream using the repository
                await _livestreamRepository.DeleteAsync(id);
            }
        }
    }
}
