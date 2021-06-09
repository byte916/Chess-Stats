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
            _userInfo = new UserInfo()
            {
                Rivals = new List<Rival>(),
                HardestRivals = new List<Game>(),
                TournamentStats = new List<TourStat[]>()
            };
            if (string.IsNullOrWhiteSpace(id)) return _userInfo;
            var userInfo = new Cache().GetUser(id);
            _userInfo.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();

            var tournamentsInfo = new Cache().GetTournamentInfo(id);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a").Select(n=>n.GetAttributeValue("href", "")).ToList();
            
            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, id, _userInfo.Rivals);
            }

            _userInfo.Games = _userInfo.Rivals.Sum(r => r.Games);
            _userInfo.Wins = _userInfo.Rivals.Sum(r => r.Wins);
            _userInfo.Draws = _userInfo.Rivals.Sum(r => r.Draws);
            _userInfo.Loses = _userInfo.Rivals.Sum(r => r.Loses);
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


        public void GetTournament(string url, string currentUserId, List<Rival> rivals)
        {
            var tournamentId = url.Replace("/tournaments/", "");
            var userInfo = new Cache().GetTournament(tournamentId);
            var users = userInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");
            // Строка с текущими пользователями
            var currentUser = users.First(n =>
                n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + currentUserId);

            var tournamentInfo = userInfo.DocumentNode.SelectNodes("//div[contains(@class, 'panel-default')]//li");
            var tournamentDate = tournamentInfo.FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Дата проведения:" || c.InnerText == "Даты проведения:"))?.GetDirectInnerText();
            var tournamentName = userInfo.DocumentNode.SelectSingleNode("//h1[contains(@class, 'page-header')]").GetDirectInnerText();
            var tournamentType = tournamentInfo
                .FirstOrDefault(t => t.ChildNodes.Any(c => c.InnerText == "Метод жеребьёвки:"))?
                .GetDirectInnerText();

            if (tournamentType!= null && !tournamentType.Contains("Швейцарская")) return;
            
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
                if (tourResult == "+")
                {
                    continue;
                }
                var rivalIndex = int.Parse(tourResult.Substring(0, tourResult.IndexOfAny(new[] {'б', 'ч'})));
                var rivalRow = users[rivalIndex];
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
