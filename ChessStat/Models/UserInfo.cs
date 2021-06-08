using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChessStat.Models
{
    public class UserInfo
    {
        /// <summary> Имя шахматиста </summary>
        public string Name { get; set; }
        public  List<Rival> Rivals { get; set; }
    }

    public class Rival
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Games { get; set; }
    }
}
