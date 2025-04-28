using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Games
{
    public class GameApplicationAutoMapperProfile : Profile
    {
        public GameApplicationAutoMapperProfile() 
        {
            CreateMap<Game, GameDto>();
            CreateMap<CreateGameDto, Game>().ForSourceMember(src => src.VideoFile, opt => opt.DoNotValidate()); // Ignore the video file, don't try to map it.;
            CreateMap<UpdateGameDto, Game>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


        }
    }
}
