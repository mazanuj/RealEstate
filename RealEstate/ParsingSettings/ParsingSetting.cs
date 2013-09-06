using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealEstate.Parsing;

namespace RealEstate.ParsingSettings
{
    public class ParseSetting
    {
        public int Id { get; set; }

        public string City { get; set; }

        public virtual ICollection<ParsingSource> Sources { get; set; } //+

    }

    public class ParsingSource
    {
        public int Id {get; set;}

        public string URL {get; private set;}

        // only for filter
        public string Site { get; set; }
        public RealEstateType RealEstateType { get; set; } 
        public Usedtype Usedtype { get; set; } 
        public AdvertType AdvertType { get; set; }

        public void Recognize(string url)
        {
            var parts = url.Replace("http://","").Split('/');
        }
    }
}
