using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using System.Web;
using RealEstate.Utils;

namespace RealEstate.Parsing.Parsers
{
    public abstract class ParserBase
    {
        private const int DEFAULTTIMEOUT = 3000;

        public abstract List<AdvertHeader> LoadHeaders(string url, DateTime toDate, int maxCount);

        public abstract Advert Parse(AdvertHeader header, WebProxy proxy, CancellationToken ct, PauseToken pt);

        CookieContainer cookie = new CookieContainer();

        public string DownloadPage(string url, string userAgent, WebProxy proxy, CancellationToken cs)
        {
            string HtmlResult = null;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;
            myHttpWebRequest.Timeout = DEFAULTTIMEOUT;

            if (cs.IsCancellationRequested)
            {
                myHttpWebRequest.Abort();
                throw new OperationCanceledException();
            }

            var myHttpWebResponse = myHttpWebRequest.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream());
            HtmlResult = sr.ReadToEnd();
            sr.Close();

            return HtmlResult;

        }

        public byte[] DownloadImage(string url, string userAgent, WebProxy proxy, CancellationToken cs, string referer)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;
            myHttpWebRequest.Referer = referer;
            myHttpWebRequest.Timeout = DEFAULTTIMEOUT;

            if (cs.IsCancellationRequested)
            {
                myHttpWebRequest.Abort();
                throw new OperationCanceledException();
            }

            byte[] result;
            byte[] buffer = new byte[4096];

            using (WebResponse response = myHttpWebRequest.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        int count = 0;
                        do
                        {
                            count = responseStream.Read(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, count);

                        } while (count != 0);

                        result = memoryStream.ToArray();

                    }
                }
            }

            return result;
        }

        protected string Normalize(string htmlValue)
        {
            return HttpUtility.HtmlDecode(htmlValue).Trim();
        }
    }
}
