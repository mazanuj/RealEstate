using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealEstate.Exporting;
using RealEstate.Parsing;

namespace RealEstate.Parsing
{
    public class ParserSetting
    {
        public int Id { get; set; }

        public string City { get; set; }

        public RealEstateType RealEstateType { get; set; }
        public Usedtype Usedtype { get; set; }
        public AdvertType AdvertType { get; set; }
        public ParsePeriod ParsePeriod { get; set; }
        public ImportSite ImportSite { get; set; }

        public ExportSite ExportSite {get; set;}
        public virtual ICollection<ParserSourceUrl> Urls { get; set; }

    }

    public class ParserSourceUrl
    {
        public int Id {get; set;}
        public string Url { get; set; }

        public ParserSetting ParserSetting { get; set; }
    }

    public enum ParsePeriod
    {
        Today,
        Yesterday,
        Week,
        All
    }

    public enum ImportSite
    {
        All,
        Avito,
        Hands
    }
}
