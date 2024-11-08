using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace web_backend.Enums
{
    public enum EventType
    {
        [Description("Football")]
        Football,
        [Description("Basketball")]
        Basketball,
        [Description("Baseball")]
        Baseball,
        [Description("Softball")]
        Softball,
        [Description("Lacrosse")]
        Lacrosse,
        [Description("Rugby")]
        Rugby,
        [Description("Soccer")]
        Soccer,
        [Description("Tennis")]
        Tennis,
        [Description("Volleyball")]
        Volleyball,
        [Description("Hockey")]
        Hockey
    }

}
