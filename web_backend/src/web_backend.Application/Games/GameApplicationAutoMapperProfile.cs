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
            CreateMap<CreateGameDto, Game>();
            CreateMap<UpdateGameDto, Game>().ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));


        }
    }
}
