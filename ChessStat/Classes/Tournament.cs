using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChessStat.Models;
using HtmlAgilityPack;

namespace ChessStat.Classes
{
    public class Tournament
    {
        
        public UserInfo Get(string id)
        {
            var userInfoUrl = "https://ratings.ruchess.ru/people/" + id;
            var userInfo = new HtmlWeb().Load(userInfoUrl);
            var result = new UserInfo()
            {
                Rivals = new List<Rival>()
            };
            result.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();

            var tournamentsUrl = userInfoUrl + "/tournaments";
            var tournamentsInfo = new HtmlWeb().Load(tournamentsUrl);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a").Select(n=>n.GetAttributeValue("href", "")).ToList();
            
            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, id, result.Rivals);
            }

            result.Rivals = result.Rivals.OrderByDescending(r => r.Games).ToList();
            return result;
        }

        public void GetTournament(string url, string currentUserId, List<Rival> rivals)
        {
            var tournamentUrl = "https://ratings.ruchess.ru/" + url;
            var userInfo = new HtmlWeb().Load(tournamentUrl);
            var users = userInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]//tr");
            // Строка с текущими пользователями
            var currentUser = users.First(n =>
                n.ChildNodes[2].FirstChild.GetAttributeValue("href", "") == "/people/" + currentUserId);
            
            for (var i = 4; i < currentUser.ChildNodes.Count-6; i++)
            {
                var innerText = currentUser.ChildNodes[i].GetDirectInnerText();
                if (innerText == "+") continue;
                var rivalIndex = int.Parse(innerText.Substring(0, innerText.IndexOfAny(new[] {'б', 'ч'})));
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

                if (innerText.EndsWith('1')) rival.Wins++;
                else if (innerText.EndsWith('0')) rival.Loses++;
                else rival.Draws++;
                rival.Games++;
            }
        }
    }
}
