using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Parsing.Parsers
{
    public static class ParsersFactory
    {
        public static ParserBase GetParser(ImportSite site)
        {
            switch (site)
            {
                case ImportSite.Avito:
                    return new AvitoParser();
                case ImportSite.Hands:
                    return new HandsParser();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
