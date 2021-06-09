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

        /// <summary> Список самых рейтинговых соперников которые были обыграны </summary>
        public List<Game> HardestRivals { get; set; }
        public List<TourStat[]> TournamentStats { get; set; }
        public List<InconvenientOpponent> InconvenientOpponent { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
    }

    public class TourStat
    {
        public decimal AvgElo { get; set; }
        public decimal PointPercent { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
        public int Games { get; set; }
    }

    public class Rival
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
    }

    public class InconvenientOpponent
    {
        public string Name { get; set; }
        /// <summary> Процент набранных очков </summary>
        public decimal Points { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
        public int Games { get; set; }
    }

    /// <summary> Описание игры </summary>
    public class Game
    {
        public string Id { get; set; }
        /// <summary> Имя соперника </summary>
        public string Name { get; set; }
        /// <summary> Сила соперника </summary>
        public int Elo { get; set; }
        /// <summary> Дата игры </summary>
        public string Date { get; set; }
        /// <summary> Название турнира </summary>
        public string Tournament { get; set; }
        /// <summary> Цвет </summary>
        public string Color { get; set; }
    }
}
