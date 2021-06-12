using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChessStat.Models;
using HtmlAgilityPack;

namespace ChessStat.Classes
{
    public class UserInfo
    {
        public StatsReportModel Get(string id)
        {
            id = id.Trim();
            var statsReportModel = new StatsReportModel()
            {
                Rivals = new List<Rival>(),
                HardestRivals = new List<Game>(),
                TournamentStats = new List<TourStat[]>(),
                InconvenientOpponent = new List<InconvenientOpponent>()
            };
            if (string.IsNullOrWhiteSpace(id)) return statsReportModel;
            GetTournamentsList(id, 1, statsReportModel.Rivals, statsReportModel.TournamentStats, statsReportModel.HardestRivals);

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
        private void GetTournamentsList(string userId, int page, List<Rival> rivals, List<TourStat[]> tournamentsStats, List<Game> hardestRivals)
        {
            var tournamentsInfo = new Cache().GetTournamentInfo(userId, page);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a")?
                .Select(n => n.GetAttributeValue("href", "")).ToList();
            if (tournaments == null) return;

            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, userId, rivals, tournamentsStats, hardestRivals);
            }

            var nextPage =
                tournamentsInfo.DocumentNode.SelectSingleNode(
                    "//ul[contains(@class, 'pagination')]//li[contains(@class,'next_page')]");
            if (nextPage == null) return;
            if (nextPage.HasClass("disabled")) return;
            GetTournamentsList(userId, page + 1, rivals, tournamentsStats, hardestRivals);
        }

        public void GetTournament(string url, string currentUserId, List<Rival> rivals, List<TourStat[]> tournamentsStats, List<Game> hardestRivals)
        {
            var tournamentId = url.Replace("/tournaments/", "");
            var tournamentPage = new Cache().GetTournament(tournamentId);
            var tournamentInfo = tournamentPage.DocumentNode.SelectNodes("//div[contains(@class, 'panel-default')]//li");
            var tournamentType = tournamentInfo
                .FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Метод жеребьёвки:"))?
                .GetDirectInnerText();
            if (tournamentType == null) return;
            if (tournamentType.Contains("Швейцарская"))
            {
                CalculateSwissTournament(tournamentPage, tournamentInfo, currentUserId, rivals, tournamentsStats, hardestRivals);
            } else if (tournamentType.Contains("Круговая"))
            {
                CalculateRoundTorunament(tournamentPage, tournamentInfo, currentUserId, rivals, hardestRivals);
            }
        }

        /// <summary> Обсчитать турнир прошедший по швейцарской системе </summary>
        private void CalculateSwissTournament(HtmlDocument tournamentPage, 
            HtmlNodeCollection tournamentInfo,
            string userId, 
            List<Rival> rivals, 
            List<TourStat[]> tournamentsStats, 
            List<Game> hardestRivals)
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
                tournamentsStats.Add(tournamentStats);
            }

            for (var i = 4; i < userRow.ChildNodes.Count - 5; i++)
            {
                // Заполняем статистику по силе игры в текущем туре
                var tourIndex = i - 4;
                var tourStat = tournamentStats[tourIndex];
                if (tourStat == null)
                {
                    tourStat = new TourStat();
                    tournamentStats[tourIndex] = tourStat;
                }

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
                var rivalId = rivalRow.ChildNodes[2].FirstChild.GetAttributeValue("href", "").Replace("/people/", "");

                var rival = rivals.FirstOrDefault(r => r.Id == rivalId);
                if (rival == null)
                {
                    rival = new Rival(rivalId, rivalRow.ChildNodes[2].FirstChild.InnerText);
                    rivals.Add(rival);
                }
                var rivalRate = int.Parse(rivalRow.ChildNodes[3].InnerText);

                tourStat.Games++;
                rival.Games++;
                tourStat.AvgElo += rivalRate;

                if (tourResult.EndsWith('1'))
                {
                    tourStat.Wins++;
                    hardestRivals.Add(new Game()
                    {
                        Id = rivalId,
                        Name = rival.Name,
                        Date = tournamentDate,
                        Tournament = tournamentName,
                        Elo = rivalRate,
                        Color = tourResult.Contains('б') ? "Белые" : "Черные"
                    });

                    rival.Wins++;
                }
                else if (tourResult.EndsWith('0'))
                {
                    tourStat.Loses++;
                    rival.Loses++;
                }
                else
                {
                    tourStat.Draws++;
                    rival.Draws++;
                }
            }
        }

        /// <summary> Обсчитать круговой турнир </summary>
        private void CalculateRoundTorunament(HtmlDocument tournamentPage,
            HtmlNodeCollection tournamentInfo,
            string userId,
            List<Rival> rivals,
            List<Game> hardestRivals)
        {
            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = tournamentPage.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            var users = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");

            // Строка с пользователем и его результатами
            var userRow = users.FirstOrDefault(n => n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + userId);
            // Бывают такие турниры, где вроде бы человек играл, а в таблице его нет
            if (userRow == null) return;
            var header = tournamentPage.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]/thead/tr/th").ToList();

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
                var rivalId = rivalRow.ChildNodes[2].FirstChild.GetAttributeValue("href", "").Replace("/people/", "");

                var rival = rivals.FirstOrDefault(r => r.Id == rivalId);
                if (rival == null)
                {
                    rival = new Rival(rivalId, rivalRow.ChildNodes[2].FirstChild.InnerText);
                    rivals.Add(rival);
                }
                var rivalRate = int.Parse(rivalRow.ChildNodes[3].InnerText);
                
                rival.Games++;

                if (tourResult =="1")
                {
                    hardestRivals.Add(new Game()
                    {
                        Id = rivalId,
                        Name = rival.Name,
                        Date = tournamentDate,
                        Tournament = tournamentName,
                        Elo = rivalRate,
                        Color = userRow.ChildNodes[i].HasClass("active") ? "Черные" : "Белые"
                    });

                    rival.Wins++;
                }
                else if (tourResult == "0")
                {
                    rival.Loses++;
                }
                else
                {
                    rival.Draws++;
                }
            }
        }
    }
}
