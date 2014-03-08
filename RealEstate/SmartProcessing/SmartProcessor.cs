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

                DetectYear(advert);

                System.Reflection.PropertyInfo proper;

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
                                proper = typeof(Advert).GetProperty(rule.VerbValue);
                                if (proper != null)
                                {
                                    proper.SetValue(advert, "", null);
                                }
                                break;
                            case Verb.RemoveAfter:
                                proper = typeof(Advert).GetProperty(rule.VerbValue);
                                if (proper != null)
                                {
                                    var value = (string)proper.GetValue(advert, null);
                                    int i = value.IndexOf(rule.VerbValue2);
                                    if (i != -1)
                                    {
                                        value = value.Remove(i);
                                        proper.SetValue(advert, value, null);
                                    }
                                }
                                break;
                            case Verb.Cut:
                                proper = typeof(Advert).GetProperty(rule.VerbValue);
                                if (proper != null)
                                {
                                    var value = (string)proper.GetValue(advert, null);
                                    value = value.Replace(rule.VerbValue, "");
                                    proper.SetValue(advert, value, null);
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

        private void DetectYear(Advert advert)
        {
            try
            {
                if (String.IsNullOrEmpty(advert.BuildingYear) || advert.BuildingYear.Length != 2)
                {
                    var yearG = Regex.Match(advert.MessageFull, @"\D20(?<year>\d{2})", RegexOptions.IgnoreCase).Groups["year"];

                    if (yearG != null && yearG.Success && !string.IsNullOrEmpty(yearG.Value))
                    {
                        advert.BuildingYear = yearG.Value;
                    }
                }

                if (String.IsNullOrEmpty(advert.BuildingQuartal) || advert.BuildingQuartal.Length != 1)
                {
                    var kvG = Regex.Match(advert.MessageFull, @"(?<kv>\d)\ ?кв\.?\w*\ ? 20(?<year>\d{2})", RegexOptions.IgnoreCase).Groups["kv"];

                    if (kvG != null && kvG.Success && !string.IsNullOrEmpty(kvG.Value))
                    {
                        advert.BuildingQuartal = kvG.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error parsing");
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

            string pattern = @"\d+(\.\d+)?/\d+(\.\d+)?/\d+(\.\d+)?";
            Regex regAreaFull = new Regex(pattern, RegexOptions.IgnoreCase);
            var mat = regAreaFull.Match(advert.MessageFull);
            if(mat.Success)
            {
                var areas = mat.Value.Split('/');
                for (int i = 0; i < areas.Length; i++)
                {
                    switch (i)
                    {
                        case 0:
                            advert.AreaFull = float.Parse(areas[i]);
                            break;
                        case 1:
                            advert.AreaLiving = float.Parse(areas[i]);
                            break;
                        case 2:
                            advert.AreaKitchen = float.Parse(areas[i]);
                            break;
                    }
                }
            }

        }

        private void RemoveStreetLabel(Advert advert)
        {
            if (!String.IsNullOrEmpty(advert.Address))
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
            try
            {
                if (!String.IsNullOrEmpty(advert.Address))
                {
                    YandexMapApi api = new YandexMapApi();
                    var searchAdress = advert.Address.Replace("ул.", "");
                    if (!searchAdress.Contains(advert.City))
                        searchAdress = advert.City + ", " + searchAdress;
                    var newAddress = api.SearchObject(searchAdress);
                    if (newAddress != null)
                        advert.Address = newAddress.ToLower().Trim() == advert.City.ToLower().Trim() ? null : newAddress;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to clarify address: " + ex.Message);
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
            KladrApi api = new KladrApi();

            if (String.IsNullOrEmpty(advert.Distinct))
            {
                if (!String.IsNullOrEmpty(oldAddress))
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
                }

                if (!string.IsNullOrEmpty(advert.City) && !string.IsNullOrEmpty(advert.Address))
                {
                    advert.Distinct = api.GetDistinct(advert.City, advert.Address);
                }
            }

            if (String.IsNullOrEmpty(advert.AO) && !String.IsNullOrEmpty(advert.Distinct))
            {
                if(advert.City != null && advert.City.ToLower().Contains("москва"))
                    advert.AO = api.GetAO(advert.Distinct);
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

            if (advert.Usedtype == Usedtype.New)
            {
                total++;
                if (!String.IsNullOrEmpty(advert.BuildingYear))
                    current++;
            }

            total++;
            if (!String.IsNullOrEmpty(advert.Address))
                current++;
            //else
            //    current = 0;

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
