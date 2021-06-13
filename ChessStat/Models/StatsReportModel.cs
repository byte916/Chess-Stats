using System.Collections.Generic;
using ChessStat.Enums;

namespace ChessStat.Models
{
    public class StatsReportModel
    {
        public StatsReportModel()
        {
            Info = new CommonInfo();
            Rivals = new List<Rival>();
            HardestRivals = new List<Game>();
            TournamentStats = new List<TourStat[]>();
            InconvenientOpponent = new List<InconvenientOpponent>();
            GameStrengths = new List<GameStrength>
            {
                new GameStrength(){ Strength = OpponentStrength.Weak, Black = new GameStrengthStat(), White = new GameStrengthStat()},
                new GameStrength(){ Strength = OpponentStrength.Equal, Black = new GameStrengthStat(), White = new GameStrengthStat()},
                new GameStrength(){ Strength = OpponentStrength.Strong, Black = new GameStrengthStat(), White = new GameStrengthStat()},
            };
        }
        public CommonInfo Info { get; set; }

        public List<Rival> Rivals { get; set; }

        /// <summary> Список самых рейтинговых соперников которые были обыграны </summary>
        public List<Game> HardestRivals { get; set; }
        /// <summary> Статистика выступлений по турам </summary>
        public List<TourStat[]> TournamentStats { get; set; }
        public List<InconvenientOpponent> InconvenientOpponent { get; set; }
        public List<GameStrength> GameStrengths { get; set; }
    }

    public class CommonInfo
    {
        /// <summary> Имя шахматиста </summary>
        public string Name { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
    }

    /// <summary> Статистика по одному туру </summary>
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
        public Rival(string id, string name)
        {
            Id = id;
            Name = name;
            Games = 0;
            Wins = 0;
            Draws = 0;
            Loses = 0;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public int Games { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
    }

    public class InconvenientOpponent
    {
        public string Id { get; set; }
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

    /// <summary> Сила игры по цветам </summary>
    public class GameStrength
    {
        public OpponentStrength Strength { get; set; }
        public GameStrengthStat White { get; set; }
        public GameStrengthStat Black { get; set; }
    }

    public class GameStrengthStat
    {
        public decimal Points { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
        public int Games { get; set; }
    }

}