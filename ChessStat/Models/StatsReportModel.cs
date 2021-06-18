using System.Collections.Generic;
using ChessStat.Enums;

namespace ChessStat.Models
{
    public class StatsReportModel
    {
        public StatsReportModel()
        {
            Info = new CommonInfo();
            // Топ частых соперников
            Rivals = new List<Rival>();
            // Топ обыгранных сильных соперников
            HardestRivals = new List<Game>();
            // Статистика выступления по турам
            TournamentStats = new List<TourStat[]>();
            // Неудобные соперники
            InconvenientOpponent = new List<InconvenientOpponent>();
            // Сила игры цветами
            GameStrengths = new List<GameStrength>
            {
                new GameStrength(){ Strength = OpponentStrength.Weak, Black = new GameStrengthStat(), White = new GameStrengthStat()},
                new GameStrength(){ Strength = OpponentStrength.Equal, Black = new GameStrengthStat(), White = new GameStrengthStat()},
                new GameStrength(){ Strength = OpponentStrength.Strong, Black = new GameStrengthStat(), White = new GameStrengthStat()},
            };
            // Список игр в текущем турнире
            CurrentTournament = new CurrentTournament();
        }
        public CommonInfo Info { get; set; }

        public List<Rival> Rivals { get; set; }

        /// <summary> Список самых рейтинговых соперников которые были обыграны </summary>
        public List<Game> HardestRivals { get; set; }
        /// <summary> Статистика выступлений по турам </summary>
        public List<TourStat[]> TournamentStats { get; set; }
        public List<InconvenientOpponent> InconvenientOpponent { get; set; }
        public List<GameStrength> GameStrengths { get; set; }
        public CurrentTournament CurrentTournament { get; set; }
    }

    public class CommonInfo
    {
        /// <summary> Имя шахматиста </summary>
        public string Name { get; set; }
        public int MaxRate { get; set; }
        public string MaxDate { get; set; }
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
        /// <summary> Сила игрока </summary>
        public int PlayerElo { get; set; }
        /// <summary> Дата игры </summary>
        public string Date { get; set; }
        /// <summary> Название турнира </summary>
        public string Tournament { get; set; }
        /// <summary> Цвет </summary>
        public string Color { get; set; }
        /// <summary> Результат игры </summary>
        public string Result { get; set; }
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

    public class CurrentTournament
    {
        public string Name { get; set; }
        public string Date { get; set; }
        /// <summary> Рейт игрока </summary>
        public int Rate { get; set; }
        public List<TournamentGame> Games { get; set; }
    }

    public class TournamentGame
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary> Рейт соперника </summary>
        public int Rate { get; set; }
        public CommonStat TotalStat { get; set; }
        public string Comment { get; set; }
        /// <summary> Является ли победа новым рекордом </summary>
        public int Hardest { get; set; }
        /// <summary> Является ли соперник сильным (50+ рейтинг ЭЛО) </summary>
        public int RateDiff { get; set; }
        /// <summary> Является ли соперник неудобным </summary>
        public int Inconvenient { get; set; }
        public decimal Result { get; set; }
        public GameColor Color { get; set; }
    }

    public class CommonStat
    {
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Loses { get; set; }
    }
}