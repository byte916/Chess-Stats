using System;
using System.Collections.Generic;
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
        
        public UserInfo Get(string id)
        {
            var userInfo = GetUser(id);
            var result = new UserInfo()
            {
                Rivals = new List<Rival>()
            };
            result.Name = userInfo.DocumentNode.SelectSingleNode("//div[contains(@class, 'page-header')]/h1").GetDirectInnerText();

            var tournamentsInfo = GetTournamentInfo(id);
            var tournaments = tournamentsInfo.DocumentNode.SelectNodes("//table[contains(@class, 'table-hover')]//a").Select(n=>n.GetAttributeValue("href", "")).ToList();
            
            foreach (var tournament in tournaments)
            {
                GetTournament(tournament, id, result.Rivals);
            }

            result.Games = result.Rivals.Sum(r => r.Games);
            result.Wins = result.Rivals.Sum(r => r.Wins);
            result.Draws = result.Rivals.Sum(r => r.Draws);
            result.Loses = result.Rivals.Sum(r => r.Loses);
            result.Rivals = result.Rivals.OrderByDescending(r => r.Games).Take(20).ToList();

            return result;
        }

        public HtmlDocument GetTournamentInfo(string id)
        {
            var doc = new HtmlDocument();
            if (File.Exists("Cache/TournamentInfo/" + id))
            {
                doc.LoadHtml(File.ReadAllText("Cache/TournamentInfo/" + id));
                return doc;
            }

            var tournamentsUrl = "https://ratings.ruchess.ru/people/" + id + "/tournaments";
            doc = new HtmlWeb().Load(tournamentsUrl);

            File.WriteAllText("Cache/TournamentInfo/" + id, doc.Text);
            return doc;
        }

        public HtmlDocument GetUser(string id)
        {
            var doc = new HtmlDocument();
            if (File.Exists("Cache/Users/" + id))
            {
                doc.LoadHtml(File.ReadAllText("Cache/Users/" + id));
                return doc;
            }
            
            var userInfoUrl = "https://ratings.ruchess.ru/people/" + id;
            doc = new HtmlWeb().Load(userInfoUrl);

            File.WriteAllText("Cache/Users/" + id, doc.Text);
            return doc;
        }

        public HtmlDocument GetTournament(string url)
        {
            var id = url.Replace("/tournaments/", "");
            
            var doc = new HtmlDocument();
            if (File.Exists("Cache/Tournaments/" + id))
            {
                doc.LoadHtml(File.ReadAllText("Cache/Tournaments/" + id));
                return doc;
            }

            var tournamentUrl = "https://ratings.ruchess.ru/" + url;

            doc = new HtmlWeb().Load(tournamentUrl);
            File.WriteAllText("Cache/Tournaments/" + id, doc.Text);
            return doc;
        }

        public void GetTournament(string url, string currentUserId, List<Rival> rivals)
        {
            var userInfo = GetTournament(url);
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
