using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RealEstate.Parsing.Parsers
{
    public class HandsParser : ParserBase
    {

        public override List<AdvertHeader> LoadHeaders(string url, DateTime toDate, int maxCount)
        {
            throw new NotImplementedException();
        }

        public override Advert Parse(AdvertHeader header, System.Net.WebProxy proxy, CancellationToken ct, PauseToken pt)
        {
            throw new NotImplementedException();
        }
    }
}
