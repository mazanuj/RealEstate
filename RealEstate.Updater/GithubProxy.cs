using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;

namespace RealEstate.Updater
{
    internal sealed class GithubProxy
    {
        public string GetProgramFile()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString("https://raw.github.com/ktflabs/realestate/master/install/status.xml");
            }
        }

        public string GetAviableVersion()
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString("https://raw.github.com/ktflabs/realestate/master/install/files/version").Trim();
            }
        }

        public void DownloadFile(string filename)
        {
            if (filename.Contains('\\'))
            {
                var directories = filename.Remove(filename.LastIndexOf('\\'));
                Directory.CreateDirectory(directories);
            }

            using (WebClient client = new WebClient())
            {
                client.DownloadFile("https://raw.github.com/ktflabs/realestate/master/install/files/" + filename, filename);
            }
            RepairLineEnding(filename);
        }

        public void RepairLineEnding(string filename)
        {
            try
            {
                var fi = new FileInfo(filename);
                if (fi != null)
                {
                    if(fi.Extension == ".xml" || fi.Extension == ".txt" || fi.Extension == ".sql" 
                        || fi.Extension == ".config" ||fi.Extension == ".html" ||fi.Extension == ".ini" || fi.Extension == ".report")
                    {
                        var text = File.ReadAllText(filename);
                        File.WriteAllText(filename, text.Replace("\n", "\r\n"));
                    }
                }
            }
            catch (Exception)
            {
                
            }
        }
    }
}
