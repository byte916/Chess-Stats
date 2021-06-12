using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessStat.Enums
{
    /// <summary> Сила соперника </summary>
    public enum OpponentStrength
    {
        /// <summary> Слабые (-∞; -51] эло</summary>
        Weak = 0,
        /// <summary> Равные [-50; +50] эло </summary>
        Equal = 1,
        /// <summary> Сильные [+51; -∞) эло</summary>
        Strong = 2
    }
}