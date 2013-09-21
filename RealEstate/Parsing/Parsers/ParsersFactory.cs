using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Parsing.Parsers
{
    public static class ParsersFactory
    {
        static AvitoParser AvitoParser = null;
        static HandsParser HandsParser = null;
        static object _lock = new object();

        public static ParserBase GetParser(ImportSite site)
        {
            lock (_lock)
            {
                switch (site)
                {
                    case ImportSite.Avito:
                        if (AvitoParser == null) AvitoParser = new AvitoParser();
                        return AvitoParser;
                    case ImportSite.Hands:
                        if (HandsParser == null) HandsParser = new HandsParser();
                        return HandsParser;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }
}
