using RealEstate.Parsing;
using System.Linq;

namespace RealEstate.Modes
{
    public static class ModeManager
    {
        public static ImportSite SiteMode { get; set; }
        public static ReleaseMode Mode { get; set; }

        static ModeManager()
        {
            SiteMode = ImportSite.All;
            Mode = ReleaseMode.Release;
        }

        public static void SetMode(string[] args)
        {
            if (args.Contains("-hands"))
            {
                SiteMode = Parsing.ImportSite.Hands;
            }
            else if (args.Contains("-avito"))
            {
                SiteMode = Parsing.ImportSite.Avito;
            }

            if (args.Contains("-debug"))
                Mode = ReleaseMode.Debug;
        }
    }

    public enum ReleaseMode
    {
        Debug,
        Release
    }
}
