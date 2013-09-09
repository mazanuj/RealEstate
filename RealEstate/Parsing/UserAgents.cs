using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Parsing
{
    public static class UserAgents
    {
        private static List<string> _agents = new List<string>();
        private static int _maxcount = -1;
        private static int _index = 0;

        private static readonly object _lock = new object();

        static UserAgents()
        {
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.1; WOW64; rv:23.0) Gecko/20100101 Firefox/23.0");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.17 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.62 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (X11; CrOS i686 4319.74.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.57 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.2 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1468.0 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1467.0 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.93 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.93 Safari/537.36");
            _agents.Add(@"Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/27.0.1453.93 Safari/537.36");
            _agents.Add(@"Mozilla/4.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/5.0)");
            _agents.Add(@"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/4.0; InfoPath.2; SV1; .NET CLR 2.0.50727; WOW64)");
            _agents.Add(@"Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)");
            _agents.Add(@"Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0; chromeframe/13.0.782.215)");
            _agents.Add(@"Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.0; Trident/5.0; chromeframe/11.0.696.57)");

            _maxcount = _agents.Count - 1;
        }

        public static string GetRandomUserAgent()
        {
            lock (_lock)
            {
                if (_maxcount == _index) _index = 0;
                return _agents[_index++];
            }
        }


    }
}
