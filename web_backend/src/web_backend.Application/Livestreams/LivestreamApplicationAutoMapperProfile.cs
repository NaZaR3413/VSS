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
            CreateMap<UpdateLivestreamDto, Livestream>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // Only map non-null values
        }
    }
}
