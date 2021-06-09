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
        private const string CacheFolder = "Cache\\";
        private const string UsersFolder = CacheFolder + "Users\\";
        private const string TournamentsFolder = CacheFolder + "Tournaments\\";
        private const string TournamentInfoFolder = CacheFolder + "TournamentInfo\\";

        public Cache()
        {
            // Создаем папки для кеша
            if (!Directory.Exists(CacheFolder)) Directory.CreateDirectory(CacheFolder);
            if (!Directory.Exists(UsersFolder)) Directory.CreateDirectory(UsersFolder);
            if (!Directory.Exists(TournamentsFolder)) Directory.CreateDirectory(TournamentsFolder);
            if (!Directory.Exists(TournamentInfoFolder)) Directory.CreateDirectory(TournamentInfoFolder);
        }
        
        public HtmlDocument GetTournamentInfo(string id, int page)
        {
            var doc = new HtmlDocument();
            var fileName = TournamentInfoFolder + id + "_" + page;
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
            if (File.Exists(UsersFolder + id))
            {
                doc.LoadHtml(File.ReadAllText(UsersFolder + id));
                return doc;
            }

            var userInfoUrl = "https://ratings.ruchess.ru/people/" + id;
            doc = new HtmlWeb().Load(userInfoUrl);

            File.WriteAllText(UsersFolder + id, doc.Text);
            return doc;
        }

        public HtmlDocument GetTournament(string id)
        {
            var doc = new HtmlDocument();
            if (File.Exists(TournamentsFolder + id))
            {
                doc.LoadHtml(File.ReadAllText(TournamentsFolder + id));
                return doc;
            }

            var tournamentUrl = "https://ratings.ruchess.ru/tournaments/" + id;

            doc = new HtmlWeb().Load(tournamentUrl);
            File.WriteAllText(TournamentsFolder + id, doc.Text);
            return doc;
        }
    }
}
