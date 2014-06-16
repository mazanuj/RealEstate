using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Web;
using RealEstate.Utils;
using System.Net.Cache;
using RealEstate.Proxies;
using RealEstate.ViewModels;
using RealEstate.Settings;

namespace RealEstate.Parsing.Parsers
{
    public abstract class ParserBase
    {
        public abstract List<AdvertHeader> LoadHeaders(string url,  DateTime toDate, TaskParsingParams param, int maxAttemptCount, ProxyManager proxyManager, CancellationToken token);

        public abstract int GetTotalCount(string sourceUrl, ProxyManager proxyManager, bool useProxy, CancellationToken token);

        public abstract Advert Parse(AdvertHeader header, WebProxy proxy, CancellationToken ct, PauseToken pt, bool onlyPhone = false);

        protected CookieContainer cookie = new CookieContainer();

        protected virtual void InitCookie(string url)
        {
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.CookieContainer = cookie;
                myHttpWebRequest.Timeout = SettingsStore.DefaultTimeout;
                myHttpWebRequest.GetResponse();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Cannot download cookie. " + ex.Message, "Error!");
            }
        }

        public string DownloadPage(string url, string userAgent, WebProxy proxy, CancellationToken cs, bool useCookie = false)
        {
            string HtmlResult = null;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            if (!useCookie) cookie = new CookieContainer();
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;
            myHttpWebRequest.Timeout = SettingsStore.DefaultTimeout;
            myHttpWebRequest.KeepAlive = true;
            myHttpWebRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Refresh);
            myHttpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            myHttpWebRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate,sdch";
            myHttpWebRequest.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8";
            myHttpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            if (cs.IsCancellationRequested)
            {
                myHttpWebRequest.Abort();
                throw new OperationCanceledException();
            }

            var myHttpWebResponse = myHttpWebRequest.GetResponse();
            var stream = myHttpWebResponse.GetResponseStream();
            if (stream.CanTimeout)
                stream.ReadTimeout = SettingsStore.DefaultTimeout * 2;
            System.IO.StreamReader sr = new System.IO.StreamReader(stream);
            var task = sr.ReadToEndAsync();
            if (task.Wait(SettingsStore.DefaultTimeout))
            {
                HtmlResult = task.Result;
                myHttpWebRequest.Abort();
                sr.Close();

                return HtmlResult;
            }
            else
                throw new TimeoutException();

        }

        public byte[] DownloadImage(string url, string userAgent, WebProxy proxy, CancellationToken cs, string referer, bool isPhone, bool useCookie = false)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            myHttpWebRequest.AllowAutoRedirect = true;
            myHttpWebRequest.Proxy = proxy ?? WebRequest.DefaultWebProxy;
            if (!useCookie) cookie = new CookieContainer();
            myHttpWebRequest.CookieContainer = cookie;
            myHttpWebRequest.UserAgent = userAgent;
            myHttpWebRequest.Referer = referer;
            myHttpWebRequest.Timeout = isPhone ? SettingsStore.DefaultTimeout : 20000;
            myHttpWebRequest.KeepAlive = true;

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
