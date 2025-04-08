using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Domain.Shared.Dtos;
    public class GameTimeDto
{
    public Guid Id { get; set; }
    public string TeamA { get; set; }
    public string TeamB { get; set; }
    public DateTime Time { get; set; }
}

