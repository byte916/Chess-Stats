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
        private readonly Cache _cache = new Cache();
        /// <summary> Последний турнир, в котором участвовал игрок </summary>
        private HtmlDocument _lastToutnament;
        public StatsReportModel Get(string id, string timeControl)
        {
            var statsReportModel = new StatsReportModel();
            if (string.IsNullOrWhiteSpace(id)) return null;
            id = id.Trim();

            if (string.IsNullOrWhiteSpace(id)) return null;
            GetTournamentsList(id, 1, statsReportModel, timeControl);
            
            statsReportModel.Info = GetCommonStats(id, statsReportModel.Rivals, statsReportModel.Info);
            if (statsReportModel.Info == null) return null;
            foreach (var userInfoRival in statsReportModel.Rivals)
            {
                if (userInfoRival.Games <= 2 || userInfoRival.Loses == 0) continue;
                statsReportModel.InconvenientOpponent.Add(new InconvenientOpponent()
                {
                    Id = userInfoRival.Id,
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

            var allRivals = statsReportModel.Rivals;
            statsReportModel.Rivals = statsReportModel.Rivals.OrderByDescending(r => r.Games).Take(20).ToList();
            statsReportModel.HardestRivals = statsReportModel.HardestRivals.OrderByDescending(r => r.Elo).Take(20).ToList();

            FillLastTournament(statsReportModel, allRivals);

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
        private CommonInfo GetCommonStats(string userId, List<Rival> rivals, CommonInfo commonInfo)
        {
            var result = new CommonInfo();
            
            var userInfo = _cache.GetUser(userId);
            if (userInfo == null) return null;
            result.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();
            
            result.Games = rivals.Sum(r => r.Games);
            result.Wins = rivals.Sum(r => r.Wins);
            result.Draws = rivals.Sum(r => r.Draws);
            result.Loses = rivals.Sum(r => r.Loses);
            result.MaxRate = commonInfo.MaxRate;
            result.MaxDate = commonInfo.MaxDate;
            return result;
        }

        /// <summary> Получить список турниров на странице </summary>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="statsReportModel"></param>
        private void GetTournamentsList(string userId, int page, StatsReportModel statsReportModel, string timeControl)
        {
            var tournamentsInfo = _cache.GetTournamentInfo(userId, page);
            var tournaments = tournamentsInfo?.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]/tr");
                
            if (tournaments == null) return;
            
            foreach (var tournament in tournaments)
            {
                var tc = tournament.ChildNodes.Where(c => c.NodeType != HtmlNodeType.Text).ToArray()[1].GetDirectInnerText();
                if (timeControl != null && timeControl!= tc) continue;
                
                var url = tournament.ChildNodes.First(c=>c.NodeType != HtmlNodeType.Text).ChildNodes.First(c => c.NodeType != HtmlNodeType.Text).GetAttributeValue("href", "");
                if (statsReportModel.TimeControls.All(t=>t!= tc)) statsReportModel.TimeControls.Add(tc);

                GetTournament(url, userId, statsReportModel);
            }

            var nextPage = tournamentsInfo.DocumentNode.SelectSingleNode("//ul[contains(@class, 'pagination')]//li[contains(@class,'next_page')]");
            if (nextPage == null) return;
            if (nextPage.HasClass("disabled")) return;
            GetTournamentsList(userId, page + 1, statsReportModel, timeControl);
        }

        public void GetTournament(string url, string currentUserId, StatsReportModel statsReportModel)
        {
            var tournamentId = url.Replace("/tournaments/", "");
            var tournamentPage = _cache.GetTournament(tournamentId);
            if (tournamentPage == null) return;
            if (_lastToutnament == null) _lastToutnament = tournamentPage;
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
                CalculateSwissTournament(tournamentPage, tournamentInfo, currentUserId, statsReportModel);
            } else if (tournamentType.Contains("Круговая"))
            {
                CalculateRoundTournament(tournamentPage, tournamentInfo, currentUserId, statsReportModel);
            }
        }

        /// <summary> Обсчитать турнир прошедший по швейцарской системе </summary>
        private void CalculateSwissTournament(HtmlDocument tournamentPage, 
            HtmlNodeCollection tournamentInfo,
            string userId,
            StatsReportModel statsReportModel)
        {
            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = tournamentPage.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            var users = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");

            // Строка с пользователем и его результатами
            var userRow = users.FirstOrDefault(n => n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + userId);
            // Бывают такие турниры, где вроде бы человек играл, а в таблице его нет
            if (userRow == null) return;

            // Получаем/создаём статистику по силе игры по турам
            var tournamentStats = statsReportModel.TournamentStats.FirstOrDefault(t => t.Count() == userRow.ChildNodes.Count - 9);
            if (tournamentStats == null)
            {
                tournamentStats = new TourStat[userRow.ChildNodes.Count - 9];
                for (var i = 0; i < tournamentStats.Length; i++)
                {
                    tournamentStats[i] = new TourStat();
                }
                statsReportModel.TournamentStats.Add(tournamentStats);
            }

            var playerElo = int.Parse(userRow.ChildNodes[3].GetDirectInnerText());
            var maxElo = int.Parse(userRow.ChildNodes[userRow.ChildNodes.Count - 2].FirstChild.GetDirectInnerText());
            if (statsReportModel.Info.MaxRate <= maxElo)
            {
                statsReportModel.Info.MaxRate = maxElo;
                statsReportModel.Info.MaxDate = tournamentName + ' ' + tournamentDate;
            }
            var isFirstTournament = statsReportModel.CurrentTournament.Games == null;
            if (isFirstTournament)
            {
                statsReportModel.CurrentTournament.Rate = playerElo;
                statsReportModel.CurrentTournament.Games = new List<TournamentGame>();
            }
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
                var rival = FillRivals(statsReportModel.Rivals, rivalRow, gameResult);
                FillHardestRivals(statsReportModel.HardestRivals, rival, tournamentDate, tournamentName, rivalRate, gameColor, gameResult, playerElo);
                FillGameStrengthByColor(gameColor, statsReportModel.GameStrengths, playerElo, rivalRate, gameResult);

                if (isFirstTournament)
                {
                    statsReportModel.CurrentTournament.Games.Add(new TournamentGame
                    {
                        Name = rival.Name,
                        Rate = rivalRate,
                        Color = gameColor,
                        Id = rival.Id,
                        Result = (decimal)gameResult / 2,
                        RateDiff = playerElo - rivalRate
                    });
                }
            }
        }

        /// <summary> Обсчитать круговой турнир </summary>
        private void CalculateRoundTournament(HtmlDocument tournamentPage,
            HtmlNodeCollection tournamentInfo,
            string userId,
            StatsReportModel statsReportModel)
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

            var isFirstTournament = statsReportModel.CurrentTournament.Games == null;
            if (isFirstTournament)
            {
                statsReportModel.CurrentTournament.Games = new List<TournamentGame>();
                statsReportModel.CurrentTournament.Rate = playerElo;
            }
            // В спаренных круговых турнирах встречаются сдвоенные ячейки (когда столбец с результатами для самого себя, пример https://ratings.ruchess.ru/tournaments/18865)
            // В таких турнирах после такой ячейки надо при получении значения заголовка добавлять единицу к текущему индексу
            var addition = 0;
            for (var i = 4; i < userRow.ChildNodes.Count - 5; i++)
            {
                // Получаем результат игры в текущем туре
                var tourResult = userRow.ChildNodes[i].GetDirectInnerText();
                if (userRow.ChildNodes[i].GetAttributeValue("colspan", 1) != 1)
                    addition = userRow.ChildNodes[i].GetAttributeValue("colspan", 1) - 1;
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

                var rival = FillRivals(statsReportModel.Rivals, rivalRow, gameResult);

                FillHardestRivals(statsReportModel.HardestRivals, rival, tournamentDate, tournamentName, rivalRate, gameColor, gameResult, playerElo);
                FillGameStrengthByColor(gameColor, statsReportModel.GameStrengths, playerElo, rivalRate, gameResult);

                if (isFirstTournament)
                {
                    statsReportModel.CurrentTournament.Games.Add(new TournamentGame
                    {
                        Name = rival.Name,
                        Rate = rivalRate,
                        Color = gameColor,
                        Id = rival.Id,
                        Result = (decimal)gameResult / 2,
                        RateDiff = playerElo - rivalRate
                    });
                }
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
        private void FillHardestRivals(List<Game> hardestRivals, Rival rival, string tournamentDate, string tournamentName, int rivalElo, GameColor gameColor, GameResult result, int playerElo)
        {
            if (result== GameResult.Lose) return;
            // Если у нас список уже заполнен и все в списке сильнее текущего
            if (hardestRivals.Count >= 20 && hardestRivals.All(r=>r.Elo > rivalElo)) return;

            hardestRivals.Add(new Game()
            {
                Id = rival.Id,
                Name = rival.Name,
                Date = tournamentDate,
                Tournament = tournamentName,
                Elo = rivalElo,
                Color = gameColor.GetDescription(),
                Result = result.GetDescription(),
                PlayerElo = playerElo
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
            if (rivalElo == 1000) return;
            
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

        private void FillLastTournament(StatsReportModel statsReportModel, List<Rival> allRivals)
        {
            if (_lastToutnament == null) return;

            var tournamentInfo = _lastToutnament.DocumentNode.SelectNodes("//div[contains(@class, 'panel-default')]//li");
            statsReportModel.CurrentTournament.Date = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText().Trim();
            statsReportModel.CurrentTournament.Name = _lastToutnament.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText().Trim();
            statsReportModel.CurrentTournament.Games.ForEach(g =>
            {
                var opponent = allRivals.FirstOrDefault(r => r.Id == g.Id);
                if (opponent == null) return;
                g.TotalStat = new CommonStat()
                {
                    Wins = opponent.Wins,
                    Draws = opponent.Draws,
                    Loses = opponent.Loses
                };
                g.Hardest = statsReportModel.HardestRivals.FindIndex(r => r.Id == g.Id && r.Elo == g.Rate);
                g.Inconvenient = statsReportModel.InconvenientOpponent.FindIndex(r => r.Id == g.Id);
                var frequentOpponent = statsReportModel.Rivals.FindIndex(r => r.Id == g.Id);
                g.Comment = "";

                if (frequentOpponent != -1) g.Comment += "Соперник из ТОП-" + (frequentOpponent + 1) + " частых соперников<br>";
                if (g.Hardest != -1 && g.Result != 0) g.Comment += "Новый рекорд! Соперник из ТОП-"+ (g.Hardest+1) + " сильнейших соперников<br>";
                if (g.Inconvenient != -1) g.Comment += "Соперник из ТОП-" + (g.Inconvenient+1) + " неудобных соперников<br>";
                if (g.RateDiff < -50 && g.Hardest == -1)
                {
                    if (g.Result == 1) g.Comment += "Победа над сильным соперником, разница в силе " + g.RateDiff * -1 + "<br>";
                    else if (g.Result == 0.5m) g.Comment += "Ничья с сильным соперником, разница в силе " + g.RateDiff * -1 + "<br>";
                }
            });
            statsReportModel.CurrentTournament.Games.Add(new TournamentGame()
            {
                Name = "Итого и среднее",
                Rate = Convert.ToInt32(statsReportModel.CurrentTournament.Games.Average(g=>g.Rate)),
                Result = statsReportModel.CurrentTournament.Games.Sum(g=>g.Result),
                TotalStat = new CommonStat()
                {
                    Wins = statsReportModel.CurrentTournament.Games.Count(g => g.Result == 1),
                    Draws = statsReportModel.CurrentTournament.Games.Count(g => g.Result == 0.5m),
                    Loses = statsReportModel.CurrentTournament.Games.Count(g => g.Result == 0)
                }
            });
        }
    }
}
