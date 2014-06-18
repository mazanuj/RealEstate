using System.Reflection;
using RealEstate.Parsing;
using RealEstate.ViewModels;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                    foreach (var rule in _rulesManager.Rules.Where(r => r.Verb == Verb.Skip).Where(rule => rule.Conditions.TrueForAll(c => c.IsSatisfy(advert)) && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site)))
                    {
                        //Trace.TraceInformation("Rule match: " + rule.ToString());
                        switch (rule.Verb)
                        {
                            case Verb.Skip:
                                return false;
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

                ClearFromCity(advert);

                if (!TryParseAddress(advert))
                    ClarifyAddress(advert);

                DetectDistinct(advert, oldAddress);

                ClearAddress(advert, ref oldAddress);

                if (String.IsNullOrEmpty(advert.Address))
                {
                    TryParseAddress(advert);
                    ClarifyAddress(advert);

                    advert.Address = advert.Address ?? oldAddress;
                }        

                if (String.IsNullOrEmpty(advert.House) && String.IsNullOrEmpty(advert.HouseStroenie))
                {
                    GetHouseByRegex(advert);
                    FillAddress(advert);
                }

                //Если улица не содержит дефис (А11-5|40-летия ВЛКСМ)
                //if (!advert.Street.Contains("-"))
                    ClearAddr(advert);

                RemoveStreetLabel(advert);

                if(String.IsNullOrEmpty(advert.Distinct))
                {
                    DetectDistinct(advert, oldAddress);
                }

                DetectPrice(advert);

                DetectArea(advert);

                DetectFloors(advert);

                DetectYear(advert);

                DetectCategory(advert);

                foreach (var rule in _rulesManager.Rules.Where(rule => rule.Conditions.TrueForAll(c => c.IsSatisfy(advert)) && (rule.Site == ImportSite.All || advert.ImportSite == rule.Site)))
                {
                    //Trace.TraceInformation("Rule match: " + rule.ToString());
                    PropertyInfo proper;
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
                                var i = value.IndexOf(rule.VerbValue2);
                                if (i != -1)
                                {
                                    value = value.Remove(i);
                                    proper.SetValue(advert, value, null);
                                }
                            }
                            break;
                        case Verb.Cut:
                            proper = typeof(Advert).GetProperty(rule.VerbValue2);
                            if (proper != null)
                            {
                                var value = (string)proper.GetValue(advert, null);
                                value = value.Replace(rule.VerbValue, "");
                                proper.SetValue(advert, value, null);
                            }
                            break;
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

        private static void ClearFromCity(Advert advert)
        {
            if(!String.IsNullOrEmpty(advert.Address) && !String.IsNullOrEmpty(advert.City))
            {
                advert.Address = advert.Address.Replace(advert.City,"").Trim(new []{','});
            }
        }

        private static void ClearAddr(Advert advert)
        {            
            if (!String.IsNullOrEmpty(advert.Street) && !String.IsNullOrEmpty(advert.House))
                advert.Street = advert.Street.Replace(advert.House, "").Replace(" д","");
        }

        private static void ClearAddress(Advert advert, ref string oldAdress)
        {
            if (!String.IsNullOrEmpty(advert.Address) && !String.IsNullOrEmpty(advert.Distinct))
            {
                advert.Address = advert.Address.Replace(advert.Distinct, "").Replace(advert.City, "").TrimStart(new[] { ',' }).Trim();
                if(!String.IsNullOrEmpty(advert.Street))
                    advert.Street = advert.Street.Replace(advert.Distinct, "").Replace(advert.City, "").Replace("?","").Replace("!","").TrimStart(new[] { ',' }).Trim();
            }
            else if (!String.IsNullOrEmpty(oldAdress) && !String.IsNullOrEmpty(advert.Distinct))
            {
                oldAdress = oldAdress.Replace(advert.Distinct, "").Replace(advert.City, "").TrimStart(new[] { ',','?' }).Trim();
            }
        }

        private static void DetectFloors(Advert advert)
        {
            try
            {
                if (advert.FloorTotal != 0) return;
                var match = Regex.Match(advert.MessageFull, @"(?<floors>\d+)(\-ти\ )?этажном");
                if (!match.Success) return;
                var gr = match.Groups["floors"];
                if (gr == null) return;
                var value = gr.Value;
                short floors;
                if (short.TryParse(value, out floors))
                    advert.FloorTotal = floors;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error detecting floors");
                Trace.WriteLine(ex.ToString());
            }
        }

        private static void DetectCategory(Advert advert)
        {
            if (!String.IsNullOrEmpty(advert.MessageFull))
            {
                advert.IsFromDeveloper = advert.MessageFull.ToLower().Contains("застройщик");
            }
        }

        private static void DetectYear(Advert advert)
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

                if (!String.IsNullOrEmpty(advert.BuildingQuartal) && advert.BuildingQuartal.Length == 1) return;
                var kvG = Regex.Match(advert.MessageFull, @"(?<kv>\d)\ ?кв\.?\w*\ ? 20(?<year>\d{2})", RegexOptions.IgnoreCase).Groups["kv"];

                if (kvG != null && kvG.Success && !string.IsNullOrEmpty(kvG.Value))
                {
                    advert.BuildingQuartal = kvG.Value;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error parsing");
            }
        }

        private static void DetectArea(Advert advert)
        {
            try
            {
                if (advert.AreaFull == 0)
                {
                    var regArea = new Regex(@"пл(\.|ощадь) (?<area>\d+)\ *кв\.?\ *м\.?", RegexOptions.IgnoreCase);

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

                const string pattern = @"\d+([.,]\d+)?\s*м?/\s*\d+([.,]\d+)?\s*м?/\s*\d+([.,]\d+)?";
                var regAreaFull = new Regex(pattern, RegexOptions.IgnoreCase);
                var mat = regAreaFull.Match(advert.MessageFull);
                if (mat.Success)
                {
                    var areas = mat.Value.Split('/');
                    for (var i = 0; i < areas.Length; i++)
                    {
                        areas[i] = areas[i].Replace("м","");
                        switch (i)
                        {
                            case 0:
                                advert.AreaFull = float.Parse(areas[i].Replace(',', '.'), CultureInfo.InvariantCulture);
                                break;
                            case 1:
                                advert.AreaLiving = float.Parse(areas[i].Replace(',', '.'), CultureInfo.InvariantCulture);
                                break;
                            case 2:
                                advert.AreaKitchen = float.Parse(areas[i].Replace(',', '.'), CultureInfo.InvariantCulture);
                                break;
                        }
                    }
                }

                if (advert.AreaKitchen != 0) return;
                var match = Regex.Match(advert.MessageFull, @"кухня\s+(?<area>\d+([.,]\d+)?)");
                if (!match.Success) return;
                var gr = match.Groups["area"];
                if (gr == null) return;
                var value = gr.Value;
                float area;
                if (float.TryParse(value, out area))
                    advert.AreaKitchen = area;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Smart processor error: " + ex.Message);
                Trace.WriteLine("Url:" + advert.Url);
            }

        }

        private static void RemoveStreetLabel(Advert advert)
        {
            if (!String.IsNullOrEmpty(advert.Address))
                advert.Address = ClearFromStreet(advert.Address);

            if (!String.IsNullOrEmpty(advert.Street))
                advert.Street = ClearFromStreet(advert.Street);
        }

        private static string ClearFromStreet(string str)
        {
            return str.Replace("улица", "").Replace("Ул.", "").Replace("ул.", "").Replace(" ул", "").Replace(" ул ", "").Replace("ул ", "").Replace("УЛ", "")
                .Replace("пр-т", "").Replace("проспект", "").Replace("Пр.", "").Replace("пр.", "").Replace(" пр", "").Replace(" пр ", "").Replace(" ш", "")
                .Trim().Trim(new []{',', '.'}).Trim();
        }

        private static void RemoveAdvertisers(Advert advert)
        {
            if (!advert.MessageFull.Contains("Дата выхода объявления")) return;
            var ind = advert.MessageFull.IndexOf("Дата выхода");
            advert.MessageFull = advert.MessageFull.Substring(0, ind);
        }

        private static void ClarifyAddress(Advert advert)
        {
            try
            {
                if (String.IsNullOrEmpty(advert.Address)) return;
                var api = new YandexMapApi();
                var searchAdress = advert.Address;
                if (!searchAdress.Contains("район") || searchAdress.IndexOf("район") > searchAdress.IndexOf("ул."))
                    searchAdress = searchAdress.Replace("ул.", "");
                searchAdress = searchAdress.Replace("Округ:", "");

                if (!searchAdress.Contains(advert.City)
                    && !searchAdress.ToLower().Contains("ст.")
                    && !searchAdress.ToLower().Contains("г.")
                    && !searchAdress.ToLower().Contains("город")
                    && !searchAdress.ToLower().Contains("пос.")
                    && !searchAdress.ToLower().Contains("поселок")
                    && !searchAdress.ToLower().Contains("область"))
                    searchAdress = advert.City + ", " + searchAdress;
                string newCity;
                var newAddress = api.SearchObject(searchAdress, out newCity);
                if (newAddress != null)
                {
                    advert.Address = newAddress.ToLower().Trim() == advert.City.ToLower().Trim() ? null : newAddress;
                    if (!String.IsNullOrEmpty(newCity))
                        advert.City = newCity;
                }

                AddressDataBase.ProcessAddress(advert);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to clarify address: " + ex.Message);
                Trace.WriteLine(ex);
            }
        }

        private static void GetHouseByRegex(Advert advert)
        {
            if (advert.ImportSite != ImportSite.Avito) return;
            var regHouse = new Regex(@"д\W*(?<house>\d+)", RegexOptions.IgnoreCase);
            if (!String.IsNullOrEmpty(advert.Address))
            {
                var match = regHouse.Match(advert.Address);
                if (match.Success && match.Groups["house"].Value != "")
                {
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = match.Groups["house"].Value;
                    if (String.IsNullOrEmpty(advert.Street))
                        advert.Street = advert.Address.Replace(match.Value, "").Trim().Trim(new[] { ',' }).Trim();
                }
            }

            var regNumber = new Regex(@"\d+");
            if (!String.IsNullOrEmpty(advert.Address))
            {
                var match = regNumber.Matches(advert.Address);
                if (match.Count == 1)
                {
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = match[0].Value;
                    if (String.IsNullOrEmpty(advert.Street))
                        advert.Street = advert.Address.Replace(match[0].Value, "").Trim().Trim(new[] { ',' }).Trim();
                }
            }


            if (!String.IsNullOrEmpty(advert.MessageFull))
            {
                var match = regHouse.Match(advert.MessageFull);
                if (match.Success && match.Groups["house"].Value != "")
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = match.Groups["house"].Value;
            }

            var regHouse2 = new Regex(@"(?<house>\d+)\s*(?<housepart>\w+)?\s*$");
            if (String.IsNullOrEmpty(advert.Address)) return;
            var m = regHouse2.Match(advert.Address);
            if (!m.Success || m.Groups["house"].Value == "") return;
            if (String.IsNullOrEmpty(advert.House))
                advert.House = m.Groups["house"].Value;
            if (String.IsNullOrEmpty(advert.HousePart))
                advert.HousePart = m.Groups["housepart"].Value;
        }

        private static bool TryParseAddress(Advert advert)
        {
            var regCity = new Regex(@"кв[\w,\.,\,, \-]*\ в ([\w,\ ,\.]+),");

            var message = advert.MessageFull.ToLower();
            var m = regCity.Match(message);
            if (m.Success)
            {
                var findedCity = m.Groups[0].Value;
                if (!advert.City.Contains(findedCity))
                {
                    var inRegion = new[] { "пос. "};
                    if (inRegion.Any(findedCity.Contains))
                    {
                        //Trace.TraceInformation("Skipped as regional");
                        return false;
                    }
                }
            }

            string street = null;

            var regAdrFull = new Regex(@"((ул)|(пр(\-т)?)\.)?\s*(?<street>[\w-]+\s*[\w]*)(\s*(ул)|(пр(\-т)?)\w*)?\W+(в\ р[\w\-]+)?\W*((д\w*\W*\s*(?<house>\d+))|(ст\w*\W*\s*(?<str>\d+)))(\W*\s*[к\/]\w*\W*\s*(?<housepart>\d))?");
            if (!String.IsNullOrEmpty(advert.Address))
            {
                m = regAdrFull.Match(advert.Address);
                if (m.Success)
                {
                    if (String.IsNullOrEmpty(advert.Street))
                        advert.Street = m.Groups["street"].Value;
                    if (String.IsNullOrEmpty(advert.HouseStroenie))
                        advert.HouseStroenie = m.Groups["str"].Value;
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = m.Groups["house"].Value;
                    if (String.IsNullOrEmpty(advert.HousePart))
                        advert.HousePart = m.Groups["housepart"].Value;

                    advert.Address = advert.Street + ", "
                        + advert.House
                        + (String.IsNullOrEmpty(advert.HouseStroenie) ? null : "стр. " + advert.HouseStroenie)
                        +(String.IsNullOrEmpty(advert.HousePart) ? null : "/" + advert.HousePart);
                    return true;
                }
            }

            var regAdrFull2 = new Regex(@"(?<street>[\w\-\.]+\s?[\w\.]*)\s*(ул[\w\.]*)?(пр[\w\.\-]*)?\s*,\s*(?<house>\d+)\s*(?<housepart>[аб])?([\ \,]\s*\s*к\w*\W*(?<housepart>\d+))?", RegexOptions.IgnoreCase);
            if (!String.IsNullOrEmpty(advert.Address))
            {
                m = regAdrFull2.Match(advert.Address);
                if (m.Success)
                {
                    if (String.IsNullOrEmpty(advert.Street))
                        advert.Street = m.Groups["street"].Value;
                    if (String.IsNullOrEmpty(advert.HouseStroenie))
                        advert.HouseStroenie = m.Groups["str"].Value;
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = m.Groups["house"].Value;
                    if (String.IsNullOrEmpty(advert.HousePart))
                        advert.HousePart = m.Groups["housepart"].Value;

                    advert.Address = advert.Street + ", "
                        + advert.House
                        + (String.IsNullOrEmpty(advert.HouseStroenie) ? null : "стр. " + advert.HouseStroenie)
                        + (String.IsNullOrEmpty(advert.HousePart) ? null : "/" + advert.HousePart);

                    if (advert.HousePart == "а" || advert.HousePart == "б")
                        advert.Street = advert.Street.TrimEnd(advert.HousePart.ToArray());
                    return true;
                }
            }

            var regHouse2 = new Regex(@"\w{3}\W*(?<house>\d+)\.?\s*(?<housepart>[аб])?(\s*[\/к]\w*\s*(?<housepart>[\d]))?(?!.*\w{2})", RegexOptions.IgnoreCase);
            if (!String.IsNullOrEmpty(advert.Address))
            {
                m = regHouse2.Match(advert.Address);
                if (m.Success && m.Groups["house"].Value != "")
                {
                    if (String.IsNullOrEmpty(advert.HousePart))
                        advert.HousePart = m.Groups["housepart"].Value;
                    if (String.IsNullOrEmpty(advert.House))
                        advert.House = m.Groups["house"].Value;
                    if (String.IsNullOrEmpty(advert.Street))
                    {
                        advert.Street = advert.Address.Replace(advert.House, "")
                            .Replace(
                            (
                                (String.IsNullOrEmpty(advert.HousePart) || advert.HousePart == "а" || advert.HousePart == "б")
                                ? "aaa" : advert.HousePart), "").Trim().Trim(new[] { ',', '\\', '/', 'к' }).Trim().Replace(".кор", "").Replace(" кор", "");

                        if (advert.HousePart == "а" || advert.HousePart == "б")
                            advert.Street = advert.Street.TrimEnd(advert.HousePart.ToArray());
                    }

                    advert.Address = advert.Street + ", " + advert.House + (String.IsNullOrEmpty(advert.HousePart) ? null : "/" + advert.HousePart);
                    return true;
                }
            }

            var rCity1 = new Regex(@"ул[\.и]\s*(?<street>[\w\-]+\s?[\w]{3,10})", RegexOptions.IgnoreCase);
            m = rCity1.Match(advert.MessageFull);

            if (m.Success && m.Groups["street"].Value != "")
            {
                street = m.Groups["street"].Value;
            }


            if (String.IsNullOrEmpty(advert.Address))
            {
                var regHigh = new Regex(@"(?<highway>\w+\ +ш.)\W", RegexOptions.IgnoreCase);
                m = regHigh.Match(advert.MessageFull);
                if (m.Success && m.Groups["highway"].Value != "")
                {
                    street = m.Groups["highway"].Value;
                }
            }

            if (street == null) return false;
            var rHouse = new Regex(@"(?<house>д\.?\ *\d+\ */?\ *\d*\ *\w)\W", RegexOptions.IgnoreCase);
            m = rHouse.Match(advert.MessageFull);
            var house = "";
            if (m.Success && m.Groups["house"].Value != "")
            {
                house = ", " + m.Groups["house"].Value.Replace('/', 'к');
            }
            else
            {
                rHouse = new Regex(street.Replace(" ", @"\ ").Replace(".", @"\.").Replace(",", @"\,") + @"\ *\,\ *(?<house>\d+)", RegexOptions.IgnoreCase);
                m = rHouse.Match(advert.MessageFull);
                if (m.Success && m.Groups["house"].Value != "")
                {
                    house = ", " + m.Groups["house"].Value.Replace('/', 'к');
                }
            }
            advert.Address = street + house;

            return true;
        }

        private static void DetectDistinct(Advert advert, string oldAddress)
        {
            var api = new KladrApi();

            if (String.IsNullOrEmpty(advert.Distinct))
            {
                if (!String.IsNullOrEmpty(oldAddress))
                {
                    var parts = oldAddress.Split(',');
                    if (parts.Any())
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
                    if (advert.City == advert.Distinct)
                        advert.Distinct = null;
                }
            }

            if (!String.IsNullOrEmpty(advert.AO) || String.IsNullOrEmpty(advert.Distinct)) return;
            if (advert.City != null && advert.City.ToLower().Contains("москва"))
                advert.AO = api.GetAO(advert.Distinct);
        }

        private static void DetectPrice(Advert advert)
        {
            if (advert.Price != 0) return;
            var regPrice = new Regex(@"(?<full>(?<mln>\d+\ млн.\ *)?(?<ths>\d+\ тыс.\ *)?\d*\ *руб.)");
            var m = regPrice.Match(advert.MessageFull);
            if (!m.Success || m.Groups.Count <= 1) return;
            var strPrice = "0";
            if (m.Groups["mln"].Value == "" && m.Groups["ths"].Value == "")
                strPrice = m.Groups["full"].Value;
            else if (m.Groups["mln"].Value == "" ^ m.Groups["ths"].Value == "")
            {
                if (m.Groups["mln"].Value != "")
                    strPrice = m.Groups["mln"].Value.Replace("млн.", "000 000");
                if (m.Groups["ths"].Value != "")
                    strPrice = m.Groups["ths"].Value.Replace("тыс.", "000");
            }
            else if (m.Groups["mln"].Value != "" && m.Groups["ths"].Value != "")
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

        public static double ComputeCoverage(Advert advert)
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

            if (advert.City != null && advert.City.ToLower() == "москва")
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

        public static void FillAddress(Advert advert, bool force = false)
        {
            if (String.IsNullOrEmpty(advert.Address)) return;
            var parts = advert.Address.Split(',');
            advert.Street = parts[0];
            if (parts.Count() <= 1) return;
            var r = new Regex(@"(?<house>\d+)(?:к|\\|/)?(?<housepart>\d+)?");
            var m = r.Match(parts[1]);
            if (!m.Success) return;
            if (String.IsNullOrEmpty(advert.House) || force)
                advert.House = m.Groups["house"].Value;
            if (String.IsNullOrEmpty(advert.HousePart) || force)
                advert.HousePart = m.Groups["housepart"].Value;
        }
    }
}
