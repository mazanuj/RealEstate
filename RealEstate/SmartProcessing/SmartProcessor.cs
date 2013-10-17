using RealEstate.Parsing;
using RealEstate.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RealEstate.SmartProcessing
{
    [Export(typeof(SmartProcessor))]
    public class SmartProcessor
    {
        public void Process(Advert advert, TaskParsingParams param)
        {
            if (param.site == ImportSite.Hands)
            {
                //http://yaroslavl.irr.ru/real-estate/apartments-sale/secondary/search/rooms=1/currency=RUR/sourcefrom=6,1,4,5/date_create=today/

                if (advert.Url.Contains("sourcefrom=6,1,4,5")) //from print
                {

                }
            }
        }

        private void ParseAddress_Hands(Advert advert)
        {
            Regex regCity = new Regex(@"кв[\w,\.,\,, \-]*\ в ([\w,\ ,\.]+),");
            Regex regRestrict = new Regex(@"([\w,\ , \-]+\ р[\w,\-]+н\.)");

            bool containsObl = true;
            var m = regCity.Match(advert.MessageFull);
            if (m.Success)
            {
                var findedCity = m.Groups[0].Value;
                Trace.TraceInformation("Found city in advert message: " + findedCity);
                if (!advert.City.Contains(findedCity))
                {

                    var inRestrict = new string[] { "пос. ", "д. ", "с. " };
                    bool isRestrict = false;
                    if (inRestrict.Any(findedCity.Contains))
                        isRestrict = true;

                    containsObl = advert.City.Contains("обл");
                }
            }

            m = regRestrict.Match(advert.MessageFull);
            if (m.Success)
            {
                var foundedDestinct = m.Groups[0].Value;
                Trace.TraceInformation("Found distinct in advert message: " + foundedDestinct);

                if (containsObl)
                {
                    advert.Distinct = foundedDestinct.Trim();
                }                
            }






        }
    }
}
