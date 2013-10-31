using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Modes
{
    public static class ModeManager
    {
        public static ImportSite Mode { get; set; }

        static ModeManager()
        {
            Mode = ImportSite.All;
        }

        public static void SetMode(string[] args)
        {
            if (args.Contains("-hands"))
            {
                Mode = Parsing.ImportSite.Hands;
            }
            else if (args.Contains("-avito"))
            {
                Mode = Parsing.ImportSite.Avito;
            }
        }
    }
}
