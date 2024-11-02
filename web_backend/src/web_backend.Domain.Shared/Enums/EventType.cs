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
        [Description("Soccer")]
        Soccer,
        [Description("Volleyball")]
        Volleyball
    }
}
