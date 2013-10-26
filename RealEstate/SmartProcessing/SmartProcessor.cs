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
        private readonly RulesManager _rulesManager;

        [ImportingConstructor]
        public SmartProcessor(RulesManager rulesManager)
        {
            _rulesManager = rulesManager;
        }
        public bool Process(Advert advert, TaskParsingParams param)
        {
            if (param.site == ImportSite.Hands)
            {
                //http://yaroslavl.irr.ru/real-estate/apartments-sale/secondary/search/rooms=1/currency=RUR/sourcefrom=6,1,4,5/date_create=today/

                if (advert.Url.Contains("sourcefrom=6,1,4,5")) //from print
                {

                }
            }

            foreach (var rule in _rulesManager.Rules)
            {
                if(rule.Conditions.TrueForAll(c => c.IsSatisfy(advert)) 
                    && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site))
                {
                    switch (rule.Verb)
                    {
                        case Verb.Skip:
                            return false;
                    }
                }
            }

            return true;
        }

        private bool ParseAddress_Hands(Advert advert)
        {
            Regex regCity = new Regex(@"кв[\w,\.,\,, \-]*\ в ([\w,\ ,\.]+),");
            Regex regRestrict = new Regex(@"([\w,\ , \-]+\ р[\w,\-]+н\.)");

            var message = advert.MessageFull.ToLower();
            var m = regCity.Match(message);
            if (m.Success)
            {
                return false;
                //var findedCity = m.Groups[0].Value;
                //if (!advert.City.Contains(findedCity))
                //{
                //    var inRestrict = new string[] { "пос. ", "д. ", "с. " };
                //    bool isRestrict = false;
                //    if (inRestrict.Any(findedCity.Contains))
                //        isRestrict = true;
                //}
            }

            m = regRestrict.Match(advert.MessageFull);
            if (m.Success)
            {
                var foundedDestinct = m.Groups[0].Value;
             
            }

            return true;

        }
    }
}
