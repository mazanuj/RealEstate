using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

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

        public double GetAviableVersion()
        {
            using (WebClient client = new WebClient())
            {
                return Double.Parse(client.DownloadString("https://raw.github.com/ktflabs/realestate/master/install/files/version"), System.Globalization.NumberStyles.AllowDecimalPoint);
            }
        }
    }
}
