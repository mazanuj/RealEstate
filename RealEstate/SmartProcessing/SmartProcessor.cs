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

                if (String.IsNullOrEmpty(advert.Address)) //from print
                {
                    if (!TryParseAddress_Hands(advert)) 
                        return false;
                }
            }

            foreach (var rule in _rulesManager.Rules)
            {
                if(rule.Conditions.TrueForAll(c => c.IsSatisfy(advert)) 
                    && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site))
                {
                    Trace.TraceInformation("Rule match: " + rule.ToString());
                    switch (rule.Verb)
                    {
                        case Verb.Skip:
                            return false;
                    }
                }
            }

            return true;
        }

        private bool TryParseAddress_Hands(Advert advert)
        {
            Regex regCity = new Regex(@"кв[\w,\.,\,, \-]*\ в ([\w,\ ,\.]+),");
            Regex regAddress = new Regex(@"на\ (([\w-]+\.?\ *(\w+\.\ )*[\w-]+)(,?\ д\.?\ ?(\d+\w*))?)");

            var message = advert.MessageFull.ToLower();
            var m = regCity.Match(message);
            if (m.Success)
            {
                var findedCity = m.Groups[0].Value;
                if (!advert.City.Contains(findedCity))
                {
                    var inRegion = new string[] { "пос. ", "д. ", "с. " };
                    if (inRegion.Any(findedCity.Contains))
                    {
                        Trace.TraceInformation("Skipped as regional");
                        return false;
                    }
                }
            }

            m = regAddress.Match(advert.MessageFull);
            if(m.Success && m.Groups.Count > 5)
            {
                advert.Address = m.Groups[1].Value;
            }

            return true;

        }
    }
}
