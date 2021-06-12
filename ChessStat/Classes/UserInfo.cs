using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChessStat.Enums;
using ChessStat.Models;
using HtmlAgilityPack;

namespace ChessStat.Classes
{
    public class UserInfo
    {
        public StatsReportModel Get(string id)
        {
            id = id.Trim();
            var statsReportModel = new StatsReportModel();

            if (string.IsNullOrWhiteSpace(id)) return statsReportModel;
            GetTournamentsList(id, 1, statsReportModel.Rivals, statsReportModel.TournamentStats, statsReportModel.HardestRivals, statsReportModel.GameStrengths);

            statsReportModel.Info = GetCommonStats(id, statsReportModel.Rivals);
            
            foreach (var userInfoRival in statsReportModel.Rivals)
            {
                if (userInfoRival.Games <= 2) continue;
                statsReportModel.InconvenientOpponent.Add(new InconvenientOpponent()
                {
                    Name = userInfoRival.Name,
                    Draws = userInfoRival.Draws,
                    Games = userInfoRival.Games,
                    Loses = userInfoRival.Loses,
                    Points = userInfoRival.Wins + Convert.ToDecimal(userInfoRival.Draws)/2,
                    Wins = userInfoRival.Wins
                });
            }


            var maxGames = Convert.ToDecimal(statsReportModel.Rivals.Any() ? statsReportModel.Rivals.Max(r => r.Games): 1);
            statsReportModel.InconvenientOpponent = statsReportModel
                .InconvenientOpponent
                .OrderByDescending(o =>
                {
                    var games = Convert.ToDecimal(o.Games);
                    var gamesToTotal = games / maxGames;
                    var loseToGames = o.Loses / games;
                    var pointToGames = 1 - o.Points / games;
                    var loseAndDrawToGames = (o.Draws + o.Loses) / games;
                    var result = gamesToTotal * loseToGames * pointToGames * loseAndDrawToGames;
                    return result;
                })
                .Take(20)
                .ToList();

            statsReportModel.Rivals = statsReportModel.Rivals.OrderByDescending(r => r.Games).Take(20).ToList();
            statsReportModel.HardestRivals = statsReportModel.HardestRivals.OrderByDescending(r => r.Elo).Take(20).ToList();

            statsReportModel.TournamentStats = statsReportModel.TournamentStats.OrderByDescending(t => t.Length).ToList();
            foreach (var userInfoTournamentStat in statsReportModel.TournamentStats)
            {
                foreach (var tourStat in userInfoTournamentStat)
                {
                    if (tourStat.Games == 0)
                    {
                        tourStat.AvgElo = 0;
                        tourStat.PointPercent=0;
                        continue;
                    }
                    tourStat.AvgElo = Math.Ceiling(tourStat.AvgElo / tourStat.Games);
                    tourStat.PointPercent = Math.Ceiling((tourStat.Wins + Convert.ToDecimal(tourStat.Draws) / 2) / tourStat.Games * 100);
                }
            }
            return statsReportModel;
        }

        /// <summary>
        /// Получить общую информацию
        /// </summary>
        private CommonInfo GetCommonStats(string userId, List<Rival> rivals)
        {
            var result = new CommonInfo();
            
            var userInfo = new Cache().GetUser(userId);
            
            result.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();
            
            result.Games = rivals.Sum(r => r.Games);
            result.Wins = rivals.Sum(r => r.Wins);
            result.Draws = rivals.Sum(r => r.Draws);
            result.Loses = rivals.Sum(r => r.Loses);

            return result;
        }

        /// <summary> Получить список турниров на странице </summary>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="rivals"></param>
        /// <param name="tournamentsStats"></param>
        /// <param name="hardestRivals"></param>
        /// <param name="gameStrengths"></param>
        private void GetTournamentsList(string userId, int page, List<Rival> rivals, List<TourStat[]> tournamentsStats, List<Game> hardestRivals, List<GameStrength> gameStrengths)
        {
            var tournamentsInfo = new Cache().GetTournamentInfo(userId, page);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a")?
                .Select(n => n.GetAttributeValue("href", "")).ToList();
            if (tournaments == null) return;

            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, userId, rivals, tournamentsStats, hardestRivals, gameStrengths);
            }

            var nextPage =
                tournamentsInfo.DocumentNode.SelectSingleNode(
                    "//ul[contains(@class, 'pagination')]//li[contains(@class,'next_page')]");
            if (nextPage == null) return;
            if (nextPage.HasClass("disabled")) return;
            GetTournamentsList(userId, page + 1, rivals, tournamentsStats, hardestRivals,gameStrengths);
        }

        public void GetTournament(string url, string currentUserId, List<Rival> rivals, List<TourStat[]> tournamentsStats, List<Game> hardestRivals, List<GameStrength> gameStrengths)
        {
            var tournamentId = url.Replace("/tournaments/", "");
            var tournamentPage = new Cache().GetTournament(tournamentId);
            var tournamentInfo = tournamentPage.DocumentNode.SelectNodes("//div[contains(@class, 'panel-default')]//li");
            var tournamentType = tournamentInfo
                .FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Метод жеребьёвки:"))?
                .GetDirectInnerText();

            // Если метод не указан, пытаемся понять по заголовку таблицы тип турнира. 
            if (tournamentType == null)
            {
                if (tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]/thead/tr/th")
                    .ToList()[4].GetDirectInnerText().Contains("тур"))
                {
                    tournamentType = "Швейцарская";
                }
                else
                {
                    tournamentType = "Круговая";
                }
            }
            if (tournamentType.Contains("Швейцарская"))
            {
                CalculateSwissTournament(tournamentPage, tournamentInfo, currentUserId, rivals, tournamentsStats, hardestRivals, gameStrengths);
            } else if (tournamentType.Contains("Круговая"))
            {
                CalculateRoundTorunament(tournamentPage, tournamentInfo, currentUserId, rivals, hardestRivals, gameStrengths);
            }
        }

        /// <summary> Обсчитать турнир прошедший по швейцарской системе </summary>
        private void CalculateSwissTournament(HtmlDocument tournamentPage, 
            HtmlNodeCollection tournamentInfo,
            string userId, 
            List<Rival> rivals, 
            List<TourStat[]> tournamentsStats, 
            List<Game> hardestRivals,
            List<GameStrength> gameStrengths)
        {

            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = tournamentPage.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            var users = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");

            // Строка с пользователем и его результатами
            var userRow = users.FirstOrDefault(n => n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + userId);
            // Бывают такие турниры, где вроде бы человек играл, а в таблице его нет
            if (userRow == null) return;

            // Получаем/создаём статистику по силе игры по турам
            var tournamentStats = tournamentsStats.FirstOrDefault(t => t.Count() == userRow.ChildNodes.Count - 9);
            if (tournamentStats == null)
            {
                tournamentStats = new TourStat[userRow.ChildNodes.Count - 9];
                for (var i = 0; i < tournamentStats.Length; i++)
                {
                    tournamentStats[i] = new TourStat();
                }
                tournamentsStats.Add(tournamentStats);
            }

            var playerElo = int.Parse(userRow.ChildNodes[3].GetDirectInnerText());

            for (var i = 4; i < userRow.ChildNodes.Count - 5; i++)
            {
                // Заполняем статистику по силе игры в текущем туре
                var tourIndex = i - 4;
                
                // Получаем результат игры в текущем туре
                var tourResult = userRow.ChildNodes[i].GetDirectInnerText();
                // Если результат игры невалидный, пропускаем тур
                if (tourResult == "+" || tourResult == "1" || tourResult == "0" || tourResult == "½")
                {
                    continue;
                }

                // Получаем соперника в текущем туре
                var rivalNumber = tourResult.Substring(0, tourResult.IndexOfAny(new[] { 'б', 'ч' }));
                var rivalRow = users.First(u => u.ChildNodes[0].GetDirectInnerText() == rivalNumber);
                
                var rivalRate = int.Parse(rivalRow.ChildNodes[3].InnerText);

                var gameColor = tourResult.Contains('б') ? GameColor.Black : GameColor.White;
                var gameResult = tourResult.EndsWith('1')
                    ? GameResult.Win
                    : tourResult.EndsWith('0')
                        ? GameResult.Lose
                        : GameResult.Draw;

                FillTourStat(gameResult, tournamentStats, tourIndex, rivalRate);
                var rival = FillRivals(rivals, rivalRow, gameResult);
                FillHardestRivals(hardestRivals, rival, tournamentDate, tournamentName, rivalRate, gameColor, gameResult);
                FillGameStrengthByColor(gameColor, gameStrengths, playerElo, rivalRate, gameResult);
            }
        }

        /// <summary> Обсчитать круговой турнир </summary>
        private void CalculateRoundTorunament(HtmlDocument tournamentPage,
            HtmlNodeCollection tournamentInfo,
            string userId,
            List<Rival> rivals,
            List<Game> hardestRivals,
            List<GameStrength> gameStrengths)
        {
            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = tournamentPage.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            var users = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");

            // Строка с пользователем и его результатами
            var userRow = users.FirstOrDefault(n => n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + userId);
            // Бывают такие турниры, где вроде бы человек играл, а в таблице его нет
            if (userRow == null) return;
            var header = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]/thead/tr/th").ToList();
            var playerElo = int.Parse(userRow.ChildNodes[3].GetDirectInnerText());

            // В спаренных круговых турнирах встречаются сдвоенные ячейки (когда столбец с результатами для самого себя, пример https://ratings.ruchess.ru/tournaments/18865)
            // В таких турнирах после такой ячейки надо при получении значения заголовка добавлять единицу к текущему индексу
            var addition = 0;
            for (var i = 4; i < userRow.ChildNodes.Count - 5; i++)
            {
                // Получаем результат игры в текущем туре
                var tourResult = userRow.ChildNodes[i].GetDirectInnerText();
                if (userRow.ChildNodes[i].GetAttributeValue("colspan", 0) == 2) addition = 1;
                if (tourResult == "♞" || tourResult == "") continue;
                
                // Получаем соперника в текущем туре
                var rivalNumber = header[i + addition].GetDirectInnerText();
                var rivalRow = users.First(u => u.ChildNodes[0].GetDirectInnerText() == rivalNumber);

                var rivalRate = int.Parse(rivalRow.ChildNodes[3].InnerText);
                var gameResult = tourResult == "1"
                    ? GameResult.Win
                    : tourResult == "0"
                        ? GameResult.Lose
                        : GameResult.Draw;
                var gameColor = userRow.ChildNodes[i].HasClass("active")
                    ? GameColor.Black
                    : GameColor.White;

                var rival = FillRivals(rivals, rivalRow, gameResult);

                FillHardestRivals(hardestRivals, rival, tournamentDate, tournamentName, rivalRate, gameColor, gameResult);
                FillGameStrengthByColor(gameColor, gameStrengths, playerElo, rivalRate, gameResult);
            }
        }

        private void FillTourStat(GameResult gameResult, TourStat[] tournamentStats, int tourIndex, int rivalElo)
        {
            var tourStat = tournamentStats[tourIndex];
            if (tourStat == null)
            {
                tourStat = new TourStat();
                tournamentStats[tourIndex] = tourStat;
            }

            tourStat.Games++;
            tourStat.AvgElo += rivalElo;

            switch (gameResult)
            {
                case GameResult.Win:
                    tourStat.Wins++;
                    break;
                case GameResult.Draw:
                    tourStat.Loses++;
                    break;
                case GameResult.Lose:
                    tourStat.Draws++;
                    break;
            }
        }

        /// <summary> Заполнить информацию о сопернике </summary>
        /// <param name="rivals"></param>
        /// <param name="rivalRow"></param>
        /// <param name="gameResult"></param>
        /// <returns></returns>
        private Rival FillRivals(List<Rival> rivals, HtmlNode rivalRow, GameResult gameResult)
        {
            var rivalId = rivalRow.ChildNodes[2].FirstChild.GetAttributeValue("href", "").Replace("/people/", "");
            var rival = rivals.FirstOrDefault(r => r.Id == rivalId);
            if (rival == null)
            {
                rival = new Rival(rivalId, rivalRow.ChildNodes[2].FirstChild.InnerText);
                rivals.Add(rival);
            }

            rival.Games++;
            switch (gameResult)
            {
                case GameResult.Win:
                    rival.Wins++;
                    break;
                case GameResult.Lose:
                    rival.Loses++;
                    break;
                case GameResult.Draw:
                    rival.Draws++;
                    break;
            }

            return rival;
        }

        /// <summary> Заполнить список сложнейших игроков </summary>
        private void FillHardestRivals(List<Game> hardestRivals, Rival rival, string tournamentDate, string tournamentName, int rivalElo, GameColor gameColor, GameResult result)
        {
            if (result!= GameResult.Win) return;
            // Если у нас список уже заполнен и все в списке сильнее текущего
            if (hardestRivals.Count >= 20 && hardestRivals.All(r=>r.Elo > rivalElo)) return;

            hardestRivals.Add(new Game()
            {
                Id = rival.Id,
                Name = rival.Name,
                Date = tournamentDate,
                Tournament = tournamentName,
                Elo = rivalElo,
                Color = gameColor.GetDescription()
            });
        }

        /// <summary> Заполнить силу игры цветом </summary>
        /// <param name="color"></param>
        /// <param name="gameStrengths"></param>
        /// <param name="playerElo"></param>
        /// <param name="rivalElo"></param>
        /// <param name="result"></param>
        private void FillGameStrengthByColor(GameColor color, List<GameStrength> gameStrengths, int playerElo, int rivalElo, GameResult result)
        {
            OpponentStrength opponentStrength = OpponentStrength.Equal;
            if (rivalElo > playerElo + 50)
            {
                opponentStrength = OpponentStrength.Strong;
            }
            else if (rivalElo < playerElo - 50)
            {
                opponentStrength = OpponentStrength.Weak;
            }

            var gameStrength = color == GameColor.White
                ? gameStrengths.First(g => g.Strength == opponentStrength).White
                : gameStrengths.First(g => g.Strength == opponentStrength).Black;

            gameStrength.Games++;
            switch (result)
            {
                case GameResult.Win:
                    gameStrength.Wins++;
                    gameStrength.Points++;
                    break;
                case GameResult.Draw:
                    gameStrength.Draws++;
                    gameStrength.Points+=0.5m;
                    break;
                case GameResult.Lose:
                    gameStrength.Loses++;
                    break;
            }
        }
    }
}
