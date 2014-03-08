using RealEstate.Parsing.Parsers;
using RealEstate.Utils;
using RealEstate.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using RealEstate.Proxies;
using System.Diagnostics;

namespace RealEstate.Parsing
{
    [Export(typeof(ParsingManager))]
    public class ParsingManager
    {
        public List<AdvertHeader> LoadHeaders(TaskParsingParams param, List<ParserSetting> settings, CancellationToken ct, PauseToken pt, int maxAttemptCount, ProxyManager proxyManager)
        {
            List<AdvertHeader> headers = new List<AdvertHeader>();
            ParserBase parser = ParsersFactory.GetParser(param.site);

            foreach (var setting in settings)
            {
                if (setting.Urls != null)
                {
                    foreach (var url in setting.Urls)
                    {
                        if (pt.IsPauseRequested)
                            pt.WaitUntillPaused();

                        ct.ThrowIfCancellationRequested();

                        var hds = parser.LoadHeaders(url, setting.GetDate(), param, maxAttemptCount, proxyManager, ct);

                        headers.AddRange(hds);

                    }
                }
            }

            return headers;
        }
    }
}
