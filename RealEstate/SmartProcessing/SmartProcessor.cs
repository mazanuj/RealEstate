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
            try
            {
                foreach (var rule in _rulesManager.Rules.Where(r => r.Verb == Verb.Skip))
                {
                    if (rule.Conditions.TrueForAll(c => c.IsSatisfy(advert))
                        && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site))
                    {
                        //Trace.TraceInformation("Rule match: " + rule.ToString());
                        switch (rule.Verb)
                        {
                            case Verb.Skip:
                                return false;
                        }
                    }
                }

                if (param.site == ImportSite.Hands)
                {
                    if (String.IsNullOrEmpty(advert.Address)) //from print
                    {
                        if (!TryParseAddress_Hands(advert))
                            return false;
                    }
                }

                ClarifyAddress(advert);

                DetectDistinct(advert);

                DetectPrice(advert);

                foreach (var rule in _rulesManager.Rules)
                {
                    if (rule.Conditions.TrueForAll(c => c.IsSatisfy(advert))
                        && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site))
                    {
                        //Trace.TraceInformation("Rule match: " + rule.ToString());
                        switch (rule.Verb)
                        {
                            case Verb.Skip:
                                return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                return false;
            }
        }

        private void ClarifyAddress(Advert advert)
        {
            YandexMapApi api = new YandexMapApi();
            var newAddress = api.SearchObject(advert.City + ", " + advert.Address);
            advert.Address = newAddress.ToLower().Trim() == advert.City.ToLower().Trim() ? string.Empty : newAddress;
        }

        private bool TryParseAddress_Hands(Advert advert)
        {
            Regex regCity = new Regex(@"кв[\w,\.,\,, \-]*\ в ([\w,\ ,\.]+),");
            Regex regAddress = new Regex(@"\ на\ +(([\w-]+\.?\ *(?:\w+\.\ +)*[\w-]+)(?:,?\ *д\.?\ *(\d+\w*))?)");

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
                        //Trace.TraceInformation("Skipped as regional");
                        return false;
                    }
                }
            }

            m = regAddress.Match(advert.MessageFull);
            if (m.Success && m.Groups.Count > 3)
            {
                advert.Address = m.Groups[1].Value;
            }

            return true;

        }

        private void DetectDistinct(Advert advert)
        {
            if (!string.IsNullOrEmpty(advert.City) && !string.IsNullOrEmpty(advert.Address))
            {
                KladrApi api = new KladrApi();
                advert.Distinct = api.GetDistinct(advert.City, advert.Address);
            }
        }

        private void DetectPrice(Advert advert)
        {
            if (advert.Price == 0)
            {
                Regex regPrice = new Regex(@"(?<full>(?<mln>\d+\ млн.\ *)?(?<ths>\d+\ тыс.\ *)?\d*\ *руб.)");
                var m = regPrice.Match(advert.MessageFull);
                if (m.Success && m.Groups.Count > 1)
                {
                    string strPrice = "0";
                    if (m.Groups["mln"].Value == "" && m.Groups["ths"].Value == "")
                    {
                        strPrice = m.Groups["full"].Value;
                    }
                    else if (m.Groups["mln"].Value == "" ^ m.Groups["ths"].Value == "")
                    {
                        if (m.Groups["mln"].Value != "")
                            strPrice = m.Groups["mln"].Value.Replace("млн.", "000 000");
                        if (m.Groups["ths"].Value != "")
                            strPrice = m.Groups["ths"].Value.Replace("тыс.", "000");
                    }
                    else
                        if (m.Groups["mln"].Value != "" && m.Groups["ths"].Value != "")
                    {
                        strPrice = m.Groups["mln"].Value.Replace("млн.", "");
                        var ths = Int32.Parse(m.Groups["ths"].Value.Replace("тыс.", "").Replace(" ", ""));
                        strPrice += ths.ToString("000") + "000";
                    }
                    strPrice = strPrice.Replace("руб.","").Replace(" ","").Trim();

                    long price;
                    Int64.TryParse(strPrice, out price);
                    advert.Price = price;
                }
            }
        }
    }
}
