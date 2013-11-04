using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealEstate.Parsing;

namespace RealEstate.Exporting
{
    public class ExportSite
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Url {get;set;}
        public string City { get; set; }

        public virtual ICollection<ParserSetting> ParseSettings { get; set; }
        public virtual ICollection<Advert> Adverts { get; set; }

        public ExportSite()
        {
            ParseSettings = new List<ParserSetting>();
            Adverts = new List<Advert>();
        }
    }
}
