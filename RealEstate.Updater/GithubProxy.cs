using System;
using System.Linq;
using System.Net;
using System.IO;

namespace RealEstate.Updater
{
    internal sealed class GithubProxy
    {
        public static string GetProgramFile()
        {
            using (var client = new WebClient())
            {
                return client.DownloadString("https://github.com/mazanuj/RealEstate/tree/mazanuj/install/status.xml");
            }
        }

        public static string GetAviableVersion()
        {
            using (var client = new WebClient())
            {
                return client.DownloadString("https://github.com/mazanuj/RealEstate/tree/mazanuj/install/files/version").Trim();
            }
        }

        public void DownloadFile(string filename)
        {
            if (filename.Contains('\\'))
            {
                var directories = filename.Remove(filename.LastIndexOf('\\'));
                Directory.CreateDirectory(directories);
            }

            using (var client = new WebClient())
            {
                client.DownloadFile("https://github.com/mazanuj/RealEstate/tree/mazanuj/install/files/" + filename, filename);
            }
            RepairLineEnding(filename);
        }

        private static void RepairLineEnding(string filename)
        {
            try
            {
                var fi = new FileInfo(filename);
                if (fi.Extension != ".xml" && fi.Extension != ".txt" && fi.Extension != ".sql" &&
                    fi.Extension != ".config" && fi.Extension != ".html" && fi.Extension != ".ini" &&
                    fi.Extension != ".report") return;
                var text = File.ReadAllText(filename);
                File.WriteAllText(filename, text.Replace("\n", "\r\n"));
            }
            catch (Exception)
            {
                
            }
        }
    }
}
