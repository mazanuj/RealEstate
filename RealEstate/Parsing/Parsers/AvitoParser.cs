﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using RealEstate.Utils;
using RealEstate.Proxies;
using RealEstate.ViewModels;
using RealEstate.OCRs;
using RealEstate.Settings;
using RealEstate.City;
using Caliburn.Micro;
using Action = System.Action;

namespace RealEstate.Parsing.Parsers
{
    public class AvitoParser : ParserBase
    {
        private const string ROOT_URl = "http://www.avito.ru/";

        public static string GenerateUrl(CityWrap city, RealEstateType real, AdvertType advert, Usedtype used)
        {
            string realUrl = null;
            string advertUrl = null;
            string usedUrl = null;

            switch (real)
            {
                case RealEstateType.Apartments:
                    realUrl = "kvartiry";
                    break;
                case RealEstateType.House:
                    break;
            }

            switch (advert)
            {
                case AdvertType.All:
                    break;
                case AdvertType.Buy:
                    break;
                case AdvertType.Sell:
                    advertUrl = "prodam";
                    break;
                case AdvertType.Rent:
                    break;
                case AdvertType.Pass:
                    break;
            }

            switch (used)
            {
                case Usedtype.All:
                    break;
                case Usedtype.New:
                    usedUrl = "novostroyka";
                    break;
                case Usedtype.Used:
                    usedUrl = "vtorichka";
                    break;
            }


            return ROOT_URl + (city.AvitoKey + "/" + realUrl + "/" + advertUrl + "/" + usedUrl + "/").Replace("//", "/");
        }

        public void UpdateList(CancellationToken ct, PauseToken pt, ProxyManager proxyManager,
            BindableCollection<CityWrap> fullList, ParsingTask task)
        {
            string page = null;
            var attemt = 0;
            WebProxy proxy = null;
            HtmlNodeCollection cityLinks = null;

            while (attemt < SettingsStore.MaxParsingAttemptCount && (page == null || cityLinks == null))
            {
                attemt++;

                ct.ThrowIfCancellationRequested();

                proxy = proxyManager.GetNextProxy();
                try
                {
                    page = DownloadPage(ROOT_URl, UserAgents.GetRandomUserAgent(), proxy, ct);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(page);

                    cityLinks = doc.DocumentNode.SelectNodes("//div[contains(@class,'region-titles')]/a");
                }
                catch (Exception ex)
                {
                    Thread.Sleep(300);
                    page = null;
                    Trace.WriteLine(ex.Message, "Web Error!");
                    proxyManager.RejectProxy(proxy);
                }
            }

            if (page == null || cityLinks == null)
                throw new Exception("Can't load " + ROOT_URl);

            proxyManager.SuccessProxy(proxy);

            Trace.WriteLine("Region counts: " + cityLinks.Count);

            task.TotalCount = cityLinks.Count;

            cityLinks.AsParallel()
                .WithDegreeOfParallelism(SettingsStore.ThreadsCount)
                .WithCancellation(ct)
                .ForAll(cityLink =>
                {
                    //foreach (var cityLink in cityLinks)
                    try
                    {
                        var start = DateTime.Now;

                        ct.ThrowIfCancellationRequested();
                        if (pt.IsPauseRequested) pt.WaitUntillPaused();
                        attemt = 0;
                        page = null;

                        while (attemt < SettingsStore.MaxParsingAttemptCount && page == null)
                        {
                            attemt++;

                            ct.ThrowIfCancellationRequested();

                            proxy = proxyManager.GetNextProxy();
                            try
                            {
                                var url = "http:" + cityLink.Attributes["href"].Value;
                                if (url == "http:#") continue;
                                //Trace.WriteLine(url);
                                page = DownloadPage(url, UserAgents.GetRandomUserAgent(), proxy, ct);
                                if (page.Length < 20000 || !page.Contains("avito"))
                                    page = null;
                            }
                            catch (Exception ex)
                            {
                                Thread.Sleep(300);
                                page = null;
                                Trace.WriteLine(ex.Message, "Web Error!");
                                proxyManager.RejectProxy(proxy);
                            }
                        }

                        if (!String.IsNullOrEmpty(page))
                        {

                            proxyManager.SuccessProxy(proxy);

                            var interanlDoc = new HtmlDocument();
                            interanlDoc.LoadHtml(page);

                            var links =
                                interanlDoc.DocumentNode.SelectNodes(
                                    "//div[@data-counter-idx='0']//div[@class='catalog_counts-cl-inner']/ul/li/a");
                            if (links == null)
                            {
                                AddCityWrap(cityLink, fullList, null);
                            }
                            else
                            {
                                foreach (var link in links)
                                {
                                    AddCityWrap(link, fullList, cityLink);
                                }
                            }
                        }

                        task.PerformStep(DateTime.Now - start);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Cities parsing error");
                        Trace.WriteLine(ex);
                    }
                });
        }

        private static void AddCityWrap(HtmlNode link, BindableCollection<CityWrap> cities, HtmlNode parrent)
        {
            var city = new CityWrap {City = link.InnerText.Trim()};


            if (cities.Any(c => c.City == city.City)) return;
            if (parrent != null)
                city.Parent = parrent.InnerText.Trim();

            city.AvitoKey =
                link.Attributes["href"].Value.TrimStart(new[] {'/'}).Trim().Replace("www.avito.ru/", "").Trim();

            Application.Current.Dispatcher.Invoke((Action) (() => cities.Add(city)));
        }

        public override List<AdvertHeader> LoadHeaders(string url, DateTime toDate, TaskParsingParams param,
            int maxAttemptCount, ProxyManager proxyManager, CancellationToken token)
        {
            var headers = new List<AdvertHeader>();
            var oldCount = -1;
            var index = 0;
            var reAtempt = false;
            var attempt = 0;
            string uri = null;
            var pagingNotFound = false;

            do
            {
                if (!reAtempt)
                {
                    oldCount = headers.Count;
                    index++;
                    attempt = 0;
                }

                var notfoundCount = 0;
                reAtempt = false;
                string result = null;
                WebProxy proxy = null;

                while (attempt < maxAttemptCount)
                {
                    attempt++;
                    token.ThrowIfCancellationRequested();
                    proxy = param.useProxy ? proxyManager.GetNextProxy() : null;

                    try
                    {
                        uri = url + (url.Contains('?') ? '&' : '?') + "p=" + index;
                        Trace.WriteLine("Downloading " + uri);
                        result = DownloadPage(uri, UserAgents.GetRandomUserAgent(), proxy, token);
                        if (result.Length < 200 || !result.Contains("AVITO.ru"))
                        {
                            proxyManager.RejectProxy(proxy);
                            throw new BadResponseException();
                        }
                        break;

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Web Error!");
                        if (ex.ToString().Contains("404"))
                        {
                            notfoundCount++;
                            if (pagingNotFound || notfoundCount > 5)
                            {
                                Trace.WriteLine("Avito.LoadHeader - notfoundCount > 5 || pagingNotFound && 404", "Info");
                                return headers;
                            }
                        }
                        proxyManager.RejectProxy(proxy);
                    }
                }

                if (result == null)
                {
                    Trace.WriteLine("Can't load headers adverts", "");
                    if (attempt < maxAttemptCount)
                        reAtempt = true;
                    continue;
                }

                proxyManager.SuccessProxy(proxy);

                var page = new HtmlDocument();
                page.LoadHtml(result);

                if (page.DocumentNode.SelectSingleNode(@"//h2[contains(@class,'nulus_h2')]") != null)
                    break;

                var tiers = page.DocumentNode.SelectNodes(@"//div[contains(@class,'item ')]");
                if (tiers != null)
                {
                    headers.AddRange(from tier in tiers
                        let link = ParseLinkToFullDescription(tier)
                        let date = ParseDate(tier)
                        where date > toDate
                        select new AdvertHeader
                        {
                            DateSite = date,
                            Url = link,
                            SourceUrl = url
                        });

                    var paginator =
                        page.DocumentNode.SelectSingleNode(@"//div[contains(@class,'b-paginator clearfix j-pages')]");
                    if (paginator != null)
                    {
                        var lastButton =
                            page.DocumentNode.SelectSingleNode(
                                @"//div[contains(@class,'b-paginator')]/a[contains(@class,'last')]");
                        if (lastButton != null) continue;
                        Trace.WriteLine("Last page of adverts: " + uri);
                        break;
                    }
                    Trace.WriteLine("Paging not found", "Warning");
                    pagingNotFound = true;
                }
                else
                {
                    Trace.WriteLine("Can't find adverts");
                    if (attempt++ < maxAttemptCount) reAtempt = true;
                }
            } while ((headers.Count != oldCount || reAtempt) && headers.Count < param.MaxCount);

            return headers;
        }

        public override int GetTotalCount(string sourceUrl, ProxyManager proxyManager, bool useProxy,
            CancellationToken token)
        {

            var headers = new List<AdvertHeader>();
            bool reAtempt;
            var attempt = 0;
            const int MAX_NOTFOUND = 5;

            do
            {
                string result = null;
                reAtempt = false;
                var notFound = 0;
                WebProxy proxy = null;

                while (attempt < SettingsStore.MaxParsingAttemptCount)
                {
                    attempt++;

                    token.ThrowIfCancellationRequested();

                    proxy = useProxy ? proxyManager.GetNextProxy() : null;

                    try
                    {
                        Trace.WriteLine("Downloading " + sourceUrl);
                        result = DownloadPage(sourceUrl, UserAgents.GetRandomUserAgent(), proxy, token);
                        if (result.Length < 200 || !result.Contains("AVITO.ru"))
                        {
                            proxyManager.RejectProxyFull(proxy);
                            throw new BadResponseException();
                        }
                        break;
                    }
                    catch (WebException wex)
                    {
                        Trace.WriteLine(wex.Message, "Web error");

                        if (wex.Response != null)
                        {
                            if (((HttpWebResponse) wex.Response).StatusCode == HttpStatusCode.Forbidden)
                            {
                                proxyManager.RejectProxyFull(proxy);
                                continue;
                            }
                            if (((HttpWebResponse) wex.Response).StatusCode == HttpStatusCode.NotFound)
                            {
                                notFound++;
                                if (notFound > MAX_NOTFOUND)
                                    break;
                                continue;
                            }
                        }

                        proxyManager.RejectProxy(proxy);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Error!");
                        proxyManager.RejectProxy(proxy);
                    }
                }

                if (result == null)
                {
                    Trace.WriteLine("Can't load headers adverts", "");
                    if (attempt < SettingsStore.MaxParsingAttemptCount && notFound < MAX_NOTFOUND)
                        reAtempt = true;
                    continue;
                }

                proxyManager.SuccessProxy(proxy);

                var page = new HtmlDocument();
                page.LoadHtml(result);

                var countNode =
                    page.DocumentNode.SelectSingleNode(@"//div[@class='breadcrumbs-links']/span[@class='link-count']");
                if (countNode != null)
                    return
                        Int32.Parse(Normalize(countNode.InnerText.Replace("&nbsp;", "")).Trim().Trim(new[] {',', ' '}));

                Trace.WriteLine("Can't find adverts");
                if (attempt < SettingsStore.MaxParsingAttemptCount)
                    reAtempt = true;
            } while (reAtempt);

            throw new ParsingException("Can't find total count", "");
        }

        private void ParseSeller(Advert advert, HtmlDocument full)
        {
            var seller = full.GetElementbyId("seller");
            if (seller != null)
            {
                var strong = seller.SelectSingleNode(@".//strong");
                if (strong != null)
                    advert.Name = Normalize(strong.InnerText);

                advert.IsFromAgency = seller.InnerText.Contains("агент");
            }
            else
            {
                throw new ParsingException("Can't get seller", "");
            }
        }

        private string ParseLinkToFullDescription(HtmlNode tier)
        {
            var link = tier.SelectSingleNode(@".//h3[@class='title']/a");
            if (link != null && link.Attributes.Contains("href"))
                return "http://www.avito.ru" + Normalize(link.Attributes["href"].Value);

            throw new ParsingException("Can't get link to full description", "");
        }

        private static DateTime ParseDate(HtmlNode tier)
        {
            var whenNode = tier.SelectSingleNode(@".//div[contains(@class,'date')]");
            if (whenNode == null || string.IsNullOrEmpty(whenNode.InnerText))
                throw new ParsingException("Can't parse date information", "");
            var whenItems =
                new List<string>(whenNode.InnerText.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries));

            for (var i = 0; i < whenItems.Count; i++)
                whenItems[i] = whenItems[i].Trim().ToLower();

            whenItems.RemoveAll(String.IsNullOrEmpty);

            if (whenItems.Count != 2) throw new ParsingException("Can't parse date information", "");
            var dateString = whenItems[0];
            var timeString = whenItems[1];

            var dt = DateTime.MinValue;

            switch (dateString)
            {
                case "сегодня":
                    dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    break;
                case "вчера":
                    dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1);
                    break;
                default:
                {
                    var dateS = dateString.Split(' ');
                    if (dateS.Count() == 2)
                    {
                        int month;
                        switch (dateS[1])
                        {
                            case "янв.":
                                month = 1;
                                break;
                            case "фев.":
                                month = 2;
                                break;
                            case "мар.":
                                month = 3;
                                break;
                            case "апр.":
                                month = 4;
                                break;
                            case "мая":
                                month = 5;
                                break;
                            case "июня":
                                month = 6;
                                break;
                            case "июля":
                                month = 7;
                                break;
                            case "авг.":
                                month = 8;
                                break;
                            case "сен.":
                                month = 9;
                                break;
                            case "окт.":
                                month = 10;
                                break;
                            case "нояб.":
                                month = 11;
                                break;
                            case "дек.":
                                month = 12;
                                break;
                            default:
                                throw new ParsingException("Can't parse date information", dateString);
                        }

                        dt = new DateTime(DateTime.Now.Year, month, Int32.Parse(dateS[0]));
                        if (DateTime.Now.Month < month)
                            dt = dt.AddYears(-1);
                    }
                }
                    break;
            }

            var timeS = timeString.Split(':');
            if (timeS.Count() != 2)
                throw new ParsingException("Can't parse date information", timeString);
            dt = dt.AddHours(Int32.Parse(timeS[0]));
            dt = dt.AddMinutes(Int32.Parse(timeS[1]));

            return dt;
        }

        private void ParseTitle(HtmlDocument full, Advert advert)
        {
            var header =
                full.DocumentNode.SelectSingleNode(
                    @".//h1[contains(@class,'item_title') or contains(@class,'item_title item_title-large') or contains(@class,'h1')]");
            if (header != null)
            {
                var title = Normalize(header.InnerText);
                advert.Title = title;

                var parts = title.Split(',');
                if (parts.Length > 1)
                {
                    advert.Rooms = parts[0].Trim(new[] {">", "-к квартира"}).Trim();

                    if (parts.Length > 1)
                    {
                        float area;
                        float.TryParse(parts[1].Replace(" м²", "").Trim(), out area);
                        advert.AreaFull = area;
                    }

                    if (parts.Length <= 2) return;
                    var floors = parts[2].Replace(" эт.", "").Trim().Split('/');

                    if (floors.Length > 0)
                    {
                        int floor;
                        Int32.TryParse(floors[0], out floor);
                        advert.Floor = (short) floor;
                    }

                    if (floors.Length <= 1) return;
                    int floorfull;
                    Int32.TryParse(floors[1], out floorfull);
                    advert.FloorTotal = (short) floorfull;
                }
                else
                {
                    Console.WriteLine(title);
                    throw new ParsingException("unknow header", title);
                }

            }
            else
                throw new ParsingException("none header", "");
        }

        private void ParseCity(Advert advert, HtmlDocument full)
        {
            var cityLabel = full.GetElementbyId("map");
            if (cityLabel == null) return;
            var city =
                cityLabel.ChildNodes.Where(
                    span => span.Attributes["class"] != null && span.Attributes["class"].Value == "c-1");
            advert.City = Normalize(String.Join(", ", city.Select(c => c.InnerText))).TrimEnd(new[] {',', ' '});

            var link = cityLabel.SelectSingleNode(@"./a");
            if (link != null)
                link.Remove();

            var div = cityLabel.SelectSingleNode(@"./div");
            if (div != null)
                div.Remove();

            var parts = cityLabel.InnerText.Split(',');
            var dist = parts.FirstOrDefault(s => s.Contains("р-н "));
            if (dist != null)
                advert.Distinct = Normalize(dist).Trim().Replace("р-н ", "");

            if (advert.City.Contains(","))
                advert.City = advert.City.Split(new[] {','}).Last().Trim();
        }

        private void ParseAddress(HtmlDocument full, Advert advert)
        {
            var addressLabel =
                full.DocumentNode.SelectSingleNode(@"//div[@class='description_term']/span[text() = 'Адрес']");
            if (addressLabel != null)
            {
                var nextBlock = addressLabel.ParentNode.NextSibling;
                if (nextBlock != null)
                {
                    var addressBlock = nextBlock.NextSibling;
                    if (addressBlock != null)
                    {
                        var link = addressBlock.SelectSingleNode(@"./a");
                        if (link != null)
                            link.Remove();

                        var metro = addressBlock.SelectSingleNode(@"./span[@class='p_i_metro']");
                        if (metro != null)
                        {
                            advert.MetroStation = metro.InnerText.TrimEnd(new[] {','});
                            metro.Remove();
                            advert.Address = Normalize(addressBlock.InnerText).TrimEnd(new[] {','}).TrimStart(new[] {','}).Trim();
                        }
                        else
                        {
                            var addr = addressBlock.SelectSingleNode(@"./span[@itemprop='streetAddress']");
                            if (addr != null)
                            {
                                advert.Address = Normalize(addr.InnerText).TrimEnd(new[] {','}).TrimStart(new[] {','}).Trim();
                                addr.Remove();
                            }

                            var value = addressBlock.InnerText.Trim().Trim(new[] {','}).Trim();
                            if (value.Contains("р-н"))
                                advert.Distinct = value.Replace("р-н", "").Trim();
                            else advert.MetroStation = value;
                        }
                    }
                }
            }
            else
            {
                addressLabel =
                    full.DocumentNode.SelectSingleNode(@"//div[@class='description_term']/span[text() = 'Город']");
                if (addressLabel != null)
                {
                    var nextBlock = addressLabel.ParentNode.NextSibling;
                    if (nextBlock != null)
                    {
                        var addressBlock = nextBlock.NextSibling;
                        if (addressBlock != null)
                        {
                            var link = addressBlock.SelectSingleNode(@"./a");
                            if (link != null)
                                link.Remove();

                            var spans = addressBlock.SelectNodes(@"./span");
                            if (spans != null)
                            {
                                foreach (var span in spans)
                                {
                                    span.Remove();
                                }
                            }

                            var value = addressBlock.InnerText.Trim().Trim(new[] {','}).Trim();
                            if (value.Contains("р-н"))
                                advert.Distinct = value.Replace("р-н", "").Trim().Trim(new[] {','}).Trim();
                            else
                                advert.MetroStation = value;
                        }
                    }
                }
            }

            if (!String.IsNullOrEmpty(advert.Address)) return;
            var adress = full.DocumentNode.SelectSingleNode(@"//span[@itemprop='streetAddress']");
            if (adress != null)
                advert.Address = adress.InnerText;
        }

        private static Int64 ParsePrice(HtmlDocument full)
        {
            var block = full.DocumentNode.SelectSingleNode(@".//span[contains(@class,'p_i_price t-item-price')]/span");
            if (block == null) throw new ParsingException("Can't find price", "");
            var priceString = block.InnerText.Replace("&nbsp;", "").Replace(" руб.", "");
            if (priceString.Contains("Не указана") || priceString.Contains("Договорная"))
                return 0;
            long price;
            if (Int64.TryParse(priceString, out price))
                return price;
            throw new ParsingException("Can't parse price", block.InnerText);
        }

        private static readonly Dictionary<string, AdvertType> dealMap;
        private static readonly Dictionary<string, Usedtype> subTypeMap;
        private static readonly Dictionary<string, RealEstateType> typeMap;

        static AvitoParser()
        {
            dealMap = new Dictionary<string, AdvertType>
            {
                {"prodam", AdvertType.Sell},
                {"sdam", AdvertType.Pass}

            };

            subTypeMap = new Dictionary<string, Usedtype>
            {
                {"novostroyka", Usedtype.New},
                {"vtorichka", Usedtype.Used},
                {"", Usedtype.All}
            };

            typeMap = new Dictionary<string, RealEstateType>
            {
                {"kvartiry", RealEstateType.Apartments}
            };
        }

        private static AdvertType MapType(string param)
        {
            return dealMap.ContainsKey(param) ? dealMap[param] : AdvertType.All;
        }

        public static string MapType(AdvertType type)
        {
            return dealMap.First(t => t.Value == type).Key;
        }

        private static Usedtype MapUsedtype(string param)
        {
            return subTypeMap.ContainsKey(param) ? subTypeMap[param] : Usedtype.All;
        }

        public static string MapUsedType(Usedtype type)
        {
            return subTypeMap.First(t => t.Value == type).Key;
        }

        private static RealEstateType MapRealEstateType(string param)
        {
            return typeMap.ContainsKey(param) ? typeMap[param] : RealEstateType.All;
        }

        public static string MapRealEstateType(RealEstateType type)
        {
            return typeMap.First(t => t.Value == type).Key;
        }

        private static void ParseCategory(HtmlDocument full, Advert advert)
        {
            var nodes =
                full.DocumentNode.SelectNodes(
                    @"//div[@class='description description-expanded']/div[@class='item-params c-1']/div[1]/a");
            if (nodes == null || nodes.Count <= 0) return;
            var node = nodes.Last();

            if (!node.Attributes.Contains("href")) return;
            var href = node.Attributes["href"].Value;

            var param = href.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            var count = param.Count();
            for (var i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 1:
                        var realEstateType = MapRealEstateType(param[i]);

                        if (realEstateType != RealEstateType.All)
                            advert.RealEstateType = realEstateType;

                        break;
                    case 2:
                        var type = MapType(param[i]);

                        if (type != AdvertType.All)
                            advert.AdvertType = type;

                        if (count == 3)
                            advert.Usedtype = Usedtype.Used;

                        break;
                    case 3:
                        var usedType = MapUsedtype(param[i]);

                        if (usedType != Usedtype.All)
                            advert.Usedtype = usedType;

                        break;
                }
            }
        }

        private string ParseDescription(HtmlDocument full)
        {
            var desc = full.GetElementbyId("desc_text");
            return desc != null ? Normalize(desc.InnerText) : "";
        }

        private List<Image> ParsePhotos(HtmlDocument full)
        {
            var result = new List<Image>();

            var gallery = full.DocumentNode.SelectSingleNode("//div[@class='gallery scrollable']/div[@class='items']");
            if (gallery != null)
            {
                foreach (var link in gallery.SelectNodes("//div[contains(@class, 'll fit')]/a").TakeWhile(link => result.Count < SettingsStore.MaxCountOfImages))
                {
                    try
                    {
                        var href = link.Attributes["href"];
                        if (href == null || href.Value == null) continue;
                        var src = Normalize(href.Value).TrimStart('/');

                        result.Add(new Image {URl = "http://" + src});
                    }
                    catch (Exception)
                    {
                        if (result.Count > 0)
                            continue;

                        throw;
                    }
                }
            }
            else
            {
                var onlyOne = full.DocumentNode.SelectSingleNode("//div[@class='picture-wrapper']//img");
                if (onlyOne == null) return result;
                var src = onlyOne.Attributes["src"];
                if (src == null || src.Value == null) return result;
                var sr = Normalize(src.Value).TrimStart('/');
                result.Add(new Image {URl = "http://" + sr});
            }

            return result;
        }

        private static string ParsePhoneImageUrl(HtmlDocument full, string itemId)
        {
            var script = full.DocumentNode.SelectSingleNode("//div[@class='item']/script");
            if (script == null)
                return null;

            var urlRegex = new Regex(@"var item_url = '([^']+)").Match(script.InnerText);
            var itemRegex = new Regex(@"item_phone = '([^']+)").Match(script.InnerText);

            if (!urlRegex.Success || !itemRegex.Success) return null;
            var itemPhone = itemRegex.Groups[1].Value;

            var pre =
                (from Match item in new Regex("[0-9a-f]+", RegexOptions.IgnoreCase).Matches(itemPhone) select item.Value)
                    .ToList();

            if (Convert.ToInt64(itemId)%2 == 0)
                pre.Reverse();

            var mixed = string.Join("", pre.ToArray());
            var s = mixed.Length;
            var pkey = "";

            for (var k = 0; k < s; ++k)
                if (k%3 == 0)
                    pkey += mixed[k];

            return string.Format(@"http://www.avito.ru/items/phone/{0}?pkey={1}", itemId, pkey);
        }

        private void ParsePhone(HtmlDocument full, Advert advert, WebProxy proxy)
        {
            var itemId = Normalize(full.GetElementbyId("item_id").InnerText);

            var phoneUrl = ParsePhoneImageUrl(full, itemId);
            if (phoneUrl == null) return;
            var phoneImage = DownloadImage(phoneUrl, UserAgents.GetRandomUserAgent(), proxy, CancellationToken.None,
                advert.Url, true);

            var phoneText = OCRManager.RecognizeImage(phoneImage);

            //System.IO.File.WriteAllBytes(@"c:/test/" + phoneImage.GetHashCode() + ".png", phoneImage);

            advert.PhoneNumber = phoneText;
        }

        public override Advert Parse(AdvertHeader header, WebProxy proxy, CancellationToken ct, PauseToken pt, bool onlyPhone = false)
        {
            var advert = new Advert();

            try
            {
                advert.DateUpdate = DateTime.Now;

                advert.DateSite = header.DateSite;
                advert.Url = header.Url;
                advert.ImportSite = ImportSite.Avito;
                advert.AdvertType = AdvertType.Sell;

                var result = DownloadPage(advert.Url, UserAgents.GetRandomUserAgent(), proxy, ct, true);
                if (result.Length < 200)
                    throw new BadResponseException();

                var page = new HtmlDocument();
                page.LoadHtml(result);

                DetectClosed(page);

                if (!onlyPhone)
                {
                    ParseTitle(page, advert);

                    ParseSeller(advert, page);
                    ParseCity(advert, page);
                    ParseAddress(page, advert);

                    ParseCategory(page, advert);

                    advert.MessageFull = ParseDescription(page);

                    advert.Price = ParsePrice(page);

                    advert.Images = ParsePhotos(page);
                }

                ParsePhone(page, advert, proxy);

                return advert;
            }
            catch (ParsingException)
            {
                Trace.WriteLine(advert.Url);
                throw;
            }
        }

        private static void DetectClosed(HtmlDocument page)
        {
            var closed = page.DocumentNode.SelectSingleNode("//div[@class='alert alert-large t-item-closed']");
            if (closed != null)
                throw new ParsingException("Advert is closed", null);
        }

    }
}