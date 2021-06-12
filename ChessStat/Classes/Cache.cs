using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            doc = new HtmlWeb().Load(tournamentsUrl);

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
            doc = new HtmlWeb().Load(userInfoUrl);

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
                return doc;
            }

            var tournamentUrl = "https://ratings.ruchess.ru/tournaments/" + id;
            doc = new HtmlWeb().Load(tournamentUrl);
            File.WriteAllText(fileName, doc.Text);
            return doc;
        }
    }
}
