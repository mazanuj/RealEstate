﻿using System;

namespace RealEstate.Parsing.Parsers
{
    public static class ParsersFactory
    {
        static AvitoParser AvitoParser;
        static HandsParser HandsParser;
        static object _lock = new object();

        public static ParserBase GetParser(ImportSite site)
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
