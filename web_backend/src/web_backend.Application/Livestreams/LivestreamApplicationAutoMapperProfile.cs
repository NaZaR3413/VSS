using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Livestreams
{
    public class LivestreamApplicationAutoMapperProfile : Profile
    {
        public LivestreamApplicationAutoMapperProfile()
        {
            CreateMap<Livestream, LivestreamDto>();
            CreateMap<CreateLivestreamDto, Livestream>();
        }
    }
}
