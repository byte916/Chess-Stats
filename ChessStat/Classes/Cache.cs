using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace ChessStat.Classes
{
    public class Cache
    {
        private string CacheFolder;
        private string UsersFolder;
        private string TournamentsFolder;
        private string TournamentInfoFolder;
        private HtmlWeb htmlWeb;
        private HttpStatusCode statusCode;

        public Cache()
        {
            CacheFolder = Path.Combine(@"Cache");
            UsersFolder = Path.Combine(CacheFolder, @"Users");
            TournamentsFolder = Path.Combine(CacheFolder, @"Tournaments");
            TournamentInfoFolder = Path.Combine(CacheFolder, @"TournamentInfo");
            // Создаем папки для кеша
            if (!Directory.Exists(CacheFolder)) Directory.CreateDirectory(CacheFolder);
            if (!Directory.Exists(UsersFolder)) Directory.CreateDirectory(UsersFolder);
            if (!Directory.Exists(TournamentsFolder)) Directory.CreateDirectory(TournamentsFolder);
            if (!Directory.Exists(TournamentInfoFolder)) Directory.CreateDirectory(TournamentInfoFolder);
            htmlWeb = new HtmlWeb
            {
                PreRequest = request =>
                {
                    // Делаем таймаут 4 минуты
                    request.Timeout = 240 * 1000;
                    return true;
                },
                PostResponse = (request, response) =>
                {
                    if (response != null)
                    {
                        statusCode = response.StatusCode;
                    }
                }
            };
        }

        public HtmlDocument GetTournamentInfo(string id, int page)
        {
            var doc = new HtmlDocument();
            var fileName = Path.Combine(TournamentInfoFolder, id + "_" + page);
            if (File.Exists(fileName) && File.GetCreationTime(fileName).Date != DateTime.Now.Date)
            {
                    File.Delete(fileName);
            }
            if (File.Exists(fileName))
            {
                doc.LoadHtml(File.ReadAllText(fileName));
                return doc;
            }

            var tournamentsUrl = "https://ratings.ruchess.ru/people/" + id + "/tournaments";
            if (page > 1) tournamentsUrl += "?page=" + page;
            doc = htmlWeb.Load(tournamentsUrl);
            if (statusCode != HttpStatusCode.OK) return null;
            File.WriteAllText(fileName, doc.Text);
            File.SetCreationTime(fileName, DateTime.Now);
            return doc;
        }

        public HtmlDocument GetUser(string id)
        {
            var doc = new HtmlDocument();
            var fileName = Path.Combine(UsersFolder, id);
            if (File.Exists(fileName))
            {
                doc.LoadHtml(File.ReadAllText(fileName));
                return doc;
            }

            var userInfoUrl = "https://ratings.ruchess.ru/people/" + id;
            doc = htmlWeb.Load(userInfoUrl);
            if (statusCode != HttpStatusCode.OK) return null;

            File.WriteAllText(fileName, doc.Text);
            return doc;
        }

        public HtmlDocument GetTournament(string id)
        {
            var doc = new HtmlDocument();
            var fileName = Path.Combine(TournamentsFolder, id);
            if (File.Exists(fileName))
            {
                doc.LoadHtml(File.ReadAllText(fileName));
                if (CheckTournament(doc))
                {
                    return doc;
                }
            }

            var tournamentUrl = "https://ratings.ruchess.ru/tournaments/" + id;
            doc = htmlWeb.Load(tournamentUrl);
            if (statusCode != HttpStatusCode.OK) return null;

            if (!CheckTournament(doc))
            {
                return null;
            }

            File.WriteAllText(fileName, doc.Text);
            return doc;
        }

        /// <summary> Проверяем турнир, если ЭЛО всех игроков в конце турнира оказалось равно тысяче, значит турнир еще не обработан в РШФ </summary>
        /// <returns></returns>
        private bool CheckTournament(HtmlDocument tournament)
        {
            var head = tournament.DocumentNode.SelectNodes("//table[contains(@class, 'table-condensed')]/thead/tr");

            if (head[0].ChildNodes[^2].InnerHtml != "R<sub>нов</sub>")
            {
                return false;
            }
            return true;
        }
    }
}
