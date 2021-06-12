using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ChessStat.Enums
{
    public enum GameColor
    {
        [Description("Белые")]
        White = 1,

        [Description("Черные")]
        Black = 0
    }
}
