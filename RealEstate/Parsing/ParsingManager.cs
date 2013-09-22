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

namespace RealEstate.Parsing
{
    [Export(typeof(ParsingManager))]
    public class ParsingManager
    {
        public List<AdvertHeader> LoadHeaders(TaskParsingParams param, List<ParserSetting> settings, CancellationToken ct, PauseToken pt, WebProxy proxy, int maxAttemptCount)
        {
            List<AdvertHeader> headers = new List<AdvertHeader>();
            ParserBase parser = ParsersFactory.GetParser(param.site);

            foreach (var setting in settings)
            {
                if (headers.Count > param.MaxCount) break;

                if (setting.Urls != null)
                {
                    foreach (var url in setting.Urls)
                    {
                        if (headers.Count > param.MaxCount) break;

                        if (ct.IsCancellationRequested)
                            throw new OperationCanceledException();
                        if (pt.IsPauseRequested)
                            pt.WaitUntillPaused();

                        headers.AddRange(parser.LoadHeaders(url, proxy, setting.GetDate(), param.MaxCount, maxAttemptCount));                        
                    }
                }
            }

            return headers;
        }
    }
}
