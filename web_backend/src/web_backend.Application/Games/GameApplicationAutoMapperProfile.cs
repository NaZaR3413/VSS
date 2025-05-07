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
            /* DTO ⇄ Entity */
            CreateMap<CreateGameDto, Game>();
            CreateMap<UpdateGameDto, Game>();

            /* Entity → DTO used by the client */
            CreateMap<Game, GameDto>();
        }
    }
}
