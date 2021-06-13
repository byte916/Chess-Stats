using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace ChessStat.Enums
{
    public enum GameResult
    {
        [Description("1")]
        Win = 2,
        [Description("½")]
        Draw = 1,
        [Description("0")]
        Lose = 0
    }
}
