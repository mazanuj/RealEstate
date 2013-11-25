﻿using RealEstate.Parsing;
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

        public bool Process(Advert advert, TaskParsingParams param, bool ignoreSkip = false)
        {
            try
            {
                if (!ignoreSkip)
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
                }

                if (String.IsNullOrEmpty(advert.Address)) //from print
                {
                    if (!TryParseAddress(advert) && !ignoreSkip)
                        return false;

                }


                if (param.site == ImportSite.Hands)
                {
                    RemoveAdvertisers(advert);
                }

                var oldAddress = advert.Address;

                ClarifyAddress(advert);

                DetectDistinct(advert, oldAddress);

                if (String.IsNullOrEmpty(advert.Address))
                {
                    TryParseAddress(advert);
                    ClarifyAddress(advert);

                    advert.Address = advert.Address ?? oldAddress;
                }

                RemoveStreetLabel(advert);

                DetectPrice(advert);

                DetectArea(advert);

                FillAddress(advert);

                foreach (var rule in _rulesManager.Rules)
                {
                    if (rule.Conditions.TrueForAll(c => c.IsSatisfy(advert))
                        && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site))
                    {
                        //Trace.TraceInformation("Rule match: " + rule.ToString());
                        switch (rule.Verb)
                        {
                            case Verb.Skip:
                                if (!ignoreSkip)
                                    return false;
                                break;
                            case Verb.RemoveAll:
                                var prop = typeof(Advert).GetProperty(rule.VerbValue);
                                if (prop != null)
                                {
                                    prop.SetValue(advert, "", null);
                                }
                                break;
                            case Verb.RemoveAfter:
                                var proper = typeof(Advert).GetProperty(rule.VerbValue);
                                if (proper != null)
                                {
                                    proper.SetValue(advert, "", null);
                                }
                                break;
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

        private void DetectArea(Advert advert)
        {
            if (advert.AreaFull == 0)
            {
                Regex regArea = new Regex(@"пл(\.|ощадь) (?<area>\d+)\ *кв\.?\ *м\.?", RegexOptions.IgnoreCase);

                var m = regArea.Match(advert.MessageFull);
                if (m.Success && m.Groups["area"].Value != "")
                {
                    float fullArea;
                    if (float.TryParse(m.Groups["area"].Value, out fullArea))
                    {
                        advert.AreaFull = fullArea;
                        return;
                    }
                }
            }
        }

        private void RemoveStreetLabel(Advert advert)
        {
            advert.Address = advert.Address.Replace("улица", "").Replace("ул.", "").Trim();
        }

        private void RemoveAdvertisers(Advert advert)
        {
            if (advert.MessageFull.Contains("Дата выхода объявления"))
            {
                var ind = advert.MessageFull.IndexOf("Дата выхода");
                advert.MessageFull = advert.MessageFull.Substring(0, ind);
            }
        }

        private void ClarifyAddress(Advert advert)
        {
            if (!String.IsNullOrEmpty(advert.Address))
            {
                YandexMapApi api = new YandexMapApi();
                var newAddress = api.SearchObject(advert.City + ", " + advert.Address.Replace("ул.", ""));
                advert.Address = newAddress.ToLower().Trim() == advert.City.ToLower().Trim() ? null : newAddress;
            }
        }

        private bool TryParseAddress(Advert advert)
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
            if (m.Success && m.Groups.Count > 3 && !m.Value.Contains("берег"))
            {
                advert.Address = m.Groups[1].Value;
            }

            string street = null;

            if (String.IsNullOrEmpty(advert.Address))
            {
                Regex rCity1 = new Regex(@"(?<street>ул.\ *(?:\ *\w\.?\-?)*\ *),", RegexOptions.IgnoreCase);


                m = rCity1.Match(advert.MessageFull);

                if (m.Success && m.Groups["street"].Value != "")
                {
                    street = m.Groups["street"].Value;
                }

            }

            if (String.IsNullOrEmpty(advert.Address))
            {
                Regex regHigh = new Regex(@"(?<highway>\w+\ +ш.)\W", RegexOptions.IgnoreCase);
                m = regHigh.Match(advert.MessageFull);
                if (m.Success && m.Groups["highway"].Value != "")
                {
                    street = m.Groups["highway"].Value;
                }
            }

            if (street != null)
            {
                Regex rHouse = new Regex(@"(?<house>д\.?\ *\d+\ */?\ *\d*\ *\w)\W", RegexOptions.IgnoreCase);
                m = rHouse.Match(advert.MessageFull);
                var house = "";
                if (m.Success && m.Groups["house"].Value != "")
                {
                    house = ", " + m.Groups["house"].Value.Replace('/', 'к');
                }

                advert.Address = street + house;
            }

            return true;

        }

        private void DetectDistinct(Advert advert, string oldAddress)
        {
            if (String.IsNullOrEmpty(advert.Distinct))
            {
                var parts = oldAddress.Split(',');
                if (parts.Count() > 0)
                {
                    if (parts[0].Contains("р-н") && parts[0].Split(' ').Count() == 2)
                    {
                        advert.Distinct = parts[0].Replace("р-н", "").Trim();
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(advert.City) && !string.IsNullOrEmpty(advert.Address))
                {
                    KladrApi api = new KladrApi();
                    advert.Distinct = api.GetDistinct(advert.City, advert.Address);
                }

                if (advert.Distinct == null)
                {

                }
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
                    strPrice = strPrice.Replace("руб.", "").Replace(" ", "").Trim();

                    long price;
                    Int64.TryParse(strPrice, out price);
                    advert.Price = price;
                }
            }
        }

        public double ComputeCoverage(Advert advert)
        {
            double total = 0;
            double current = 0;

            total++;
            if (!String.IsNullOrEmpty(advert.Address))
                current++;

            total++;
            if (!String.IsNullOrEmpty(advert.Distinct))
                current++;

            total++;
            if (!String.IsNullOrEmpty(advert.Rooms))
                current++;

            total++;
            if (!String.IsNullOrEmpty(advert.PhoneNumber))
                current++;

            total++;
            if (!String.IsNullOrEmpty(advert.City))
                current++;

            total++;
            if (advert.AreaFull != 0)
                current++;

            if (advert.City.ToLower() == "москва")
            {
                total++;
                if (!String.IsNullOrEmpty(advert.MetroStation))
                    current++;
            }

            return current / total;
        }

        public void FillAddress(Advert advert)
        {
            if (!String.IsNullOrEmpty(advert.Address))
            {
                var parts = advert.Address.Split(',');
                advert.Street = parts[0];
                if (parts.Count() > 1)
                {
                    Regex r = new Regex(@"(?<house>\d+)(?:к|\\|/)?(?<housepart>\d+)?");
                    var m = r.Match(parts[1]);
                    if (m.Success)
                    {
                        advert.House = m.Groups["house"].Value;
                        advert.HousePart = m.Groups["housepart"].Value;
                    }
                }
            }
        }
    }
}
