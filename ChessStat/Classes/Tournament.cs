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
    public class Tournament
    {
        private UserInfo _userInfo;
        public UserInfo Get(string id)
        {
            id = id.Trim();
            _userInfo = new UserInfo()
            {
                Rivals = new List<Rival>(),
                HardestRivals = new List<Game>(),
                TournamentStats = new List<TourStat[]>(),
                InconvenientOpponent = new List<InconvenientOpponent>()
            };
            if (string.IsNullOrWhiteSpace(id)) return _userInfo;
            var userInfo = new Cache().GetUser(id);
            _userInfo.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();
            
            GetTournamentsFromPage(id, 1);

            _userInfo.Games = _userInfo.Rivals.Sum(r => r.Games);
            _userInfo.Wins = _userInfo.Rivals.Sum(r => r.Wins);
            _userInfo.Draws = _userInfo.Rivals.Sum(r => r.Draws);
            _userInfo.Loses = _userInfo.Rivals.Sum(r => r.Loses);
            
            foreach (var userInfoRival in _userInfo.Rivals)
            {
                if (userInfoRival.Games <= 2) continue;
                _userInfo.InconvenientOpponent.Add(new InconvenientOpponent()
                {
                    Name = userInfoRival.Name,
                    Draws = userInfoRival.Draws,
                    Games = userInfoRival.Games,
                    Loses = userInfoRival.Loses,
                    Points = userInfoRival.Wins + Convert.ToDecimal(userInfoRival.Draws)/2,
                    Wins = userInfoRival.Wins
                });
            }


            var maxGames = Convert.ToDecimal(_userInfo.Rivals.Any() ? _userInfo.Rivals.Max(r => r.Games): 1);
            _userInfo.InconvenientOpponent = _userInfo
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

            _userInfo.Rivals = _userInfo.Rivals.OrderByDescending(r => r.Games).Take(20).ToList();
            _userInfo.HardestRivals = _userInfo.HardestRivals.OrderByDescending(r => r.Elo).Take(20).ToList();

            _userInfo.TournamentStats = _userInfo.TournamentStats.OrderByDescending(t => t.Length).ToList();
            foreach (var userInfoTournamentStat in _userInfo.TournamentStats)
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
            return _userInfo;
        }

        private void GetTournamentsFromPage(string userId, int page)
        {
            var tournamentsInfo = new Cache().GetTournamentInfo(userId, page);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a").Select(n => n.GetAttributeValue("href", "")).ToList();

            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, userId, _userInfo.Rivals);
            }

            var nextPage =
                tournamentsInfo.DocumentNode.SelectSingleNode(
                    "//ul[contains(@class, 'pagination')]//li[contains(@class,'next_page')]");
            if (nextPage == null) return;
            if (nextPage.HasClass("disabled")) return;
            GetTournamentsFromPage(userId, page + 1);
        }

        public void GetTournament(string url, string currentUserId, List<Rival> rivals)
        {
            var tournamentId = url.Replace("/tournaments/", "");
            var userInfo = new Cache().GetTournament(tournamentId);
            var tournamentInfo = userInfo.DocumentNode.SelectNodes("//div[contains(@class, 'panel-default')]//li");
            var tournamentType = tournamentInfo
                .FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Метод жеребьёвки:"))?
                .GetDirectInnerText();
            if (tournamentType != null && !tournamentType.Contains("Швейцарская")) return;

            var users = userInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");
            // Строка с текущими пользователями
            var currentUser = users.FirstOrDefault(n =>
                n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + currentUserId);
            if(currentUser==null) return;
            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = userInfo.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            
            
            var tournamentStats = _userInfo.TournamentStats.FirstOrDefault(t => t.Count() == currentUser.ChildNodes.Count - 9);
            if (tournamentStats == null)
            {
                tournamentStats = new TourStat[currentUser.ChildNodes.Count - 9];
                _userInfo.TournamentStats.Add(tournamentStats);
            }
            for (var i = 4; i < currentUser.ChildNodes.Count-5; i++)
            {
                var tourIndex = i - 4;
                var tourStat = tournamentStats[tourIndex];
                if (tourStat == null)
                {
                    tourStat = new TourStat();
                    tournamentStats[tourIndex] = tourStat;
                }
                var tourResult = currentUser.ChildNodes[i].GetDirectInnerText();
                if (tourResult == "+" || tourResult == "1" || tourResult == "0" || tourResult == "½")
                {
                    continue;
                }
                var rivalIndex = tourResult.Substring(0, tourResult.IndexOfAny(new[] {'б', 'ч'}));
                var rivalRow = users.First(u=>u.ChildNodes[0].GetDirectInnerText()== rivalIndex);
                var rivalId = rivalRow.ChildNodes[2].FirstChild.GetAttributeValue("href", "").Replace("/people/", "");
                var rival = rivals.FirstOrDefault(r => r.Id == rivalId);
                if (rival == null)
                {
                    var rivalName = rivalRow.ChildNodes[2].FirstChild.InnerText;
                    rival = new Rival()
                    {
                        Id = rivalId,
                        Name = rivalName,
                        Games = 0,
                        Wins = 0,
                        Draws = 0,
                        Loses = 0
                    };
                    rivals.Add(rival);
                }

                var rivalRate = int.Parse(rivalRow.ChildNodes[3].InnerText);
                tourStat.Games++;
                tourStat.AvgElo += rivalRate;
                if (tourResult.EndsWith('1'))
                {
                    tourStat.Wins++;
                    _userInfo.HardestRivals.Add(new Game()
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
                rival.Games++;
            }
        }
    }
}
