using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;

namespace RealEstate.Parsing.Parsers
{
    public abstract class ParserBase
    {
        protected abstract Advert ParseAdvertHtml(HtmlNode advertNode);

        protected abstract List<HtmlNode> GetAdvertsNode(HtmlNode pageNode);

        public abstract List<Advert> ParsePage(string url);

        protected const int MAXCOUNT = 3;

        CookieContainer cookie = new CookieContainer();

        public string DownloadPage(string url, string userAgent, WebProxy proxy, CancellationToken cs)
        {
            Trace.WriteLine("Sending request to " + url);

            string HtmlResult = null;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;

            var asyncResult = myHttpWebRequest.BeginGetResponse(null, null);

            WaitHandle.WaitAny(new[] { asyncResult.AsyncWaitHandle, cs.WaitHandle });
            if (cs.IsCancellationRequested)
            {
                myHttpWebRequest.Abort();
                throw new OperationCanceledException();
            }

            var myHttpWebResponse = myHttpWebRequest.EndGetResponse(asyncResult);
            System.IO.StreamReader sr = new System.IO.StreamReader(myHttpWebResponse.GetResponseStream());
            HtmlResult = sr.ReadToEnd();
            sr.Close();
            Trace.WriteLine("Response is received");

            return HtmlResult;

        }

        public byte[] DownloadImage(string url, string userAgent, WebProxy proxy, CancellationToken cs, string referer)
        {
            Trace.WriteLine("Sending request to " + url);

            string HtmlResult = null;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;
            myHttpWebRequest.Referer = referer;

            var asyncResult = myHttpWebRequest.BeginGetResponse(null, null);

            WaitHandle.WaitAny(new[] { asyncResult.AsyncWaitHandle, cs.WaitHandle });
            if (cs.IsCancellationRequested)
            {
                myHttpWebRequest.Abort();
                throw new OperationCanceledException();
            }

            byte[] result;
            byte[] buffer = new byte[4096];

            using (WebResponse response = myHttpWebRequest.EndGetResponse(asyncResult))
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

            Trace.WriteLine("Response is received");

            return result;
        }
    }
}
