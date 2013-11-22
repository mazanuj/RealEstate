using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HtmlAgilityPack;
using System.Web;
using System.Text.RegularExpressions;
using System.Drawing;
using System.IO;
using RealEstate.Utils;
using RealEstate.Proxies;
using RealEstate.ViewModels;
using RealEstate.OCRs;

namespace RealEstate.Parsing.Parsers
{
    public class AvitoParser : ParserBase
    {
        const string ROOT_URl = "http://www.avito.ru/";

        public override List<AdvertHeader> LoadHeaders(ParserSourceUrl url, DateTime toDate, TaskParsingParams param, int maxAttemptCount, ProxyManager proxyManager, CancellationToken token)
        {
            List<AdvertHeader> headers = new List<AdvertHeader>();
            int oldCount = -1;
            int index = 0;
            bool reAtempt = false;

            do
            {
                if (!reAtempt)
                {
                    oldCount = headers.Count;
                    index++;
                }

                string result = null;
                int attempt = 0;

                while (attempt++ < maxAttemptCount)
                {
                    token.ThrowIfCancellationRequested();
                    WebProxy proxy = param.useProxy ? proxyManager.GetNextProxy() : null;

                    try
                    {
                        string uri = url.Url + (url.Url.Contains('?') ? '&' : '?') + "p=" + index;
                        Trace.WriteLine("Downloading " + uri);
                        result = this.DownloadPage(uri, UserAgents.GetRandomUserAgent(), proxy, CancellationToken.None);
                        if (result.Length < 200 || !result.Contains("квартир"))
                        {
                            proxyManager.RejectProxyFull(proxy);
                            throw new BadResponseException();
                        }
                        break;

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Web Error!");
                        proxyManager.RejectProxy(proxy);
                    }
                }

                if (result == null) throw new ParsingException("Can't load headers adverts", "");

                HtmlDocument page = new HtmlDocument();
                page.LoadHtml(result);

                if (page.DocumentNode.SelectSingleNode(@"//h2[contains(@class,'nulus_h2')]") != null)
                    break;

                var tiers = page.DocumentNode.SelectNodes(@"//div[contains(@class,'t_i_i')]");
                if (tiers != null)
                {
                    reAtempt = false;

                    foreach (HtmlNode tier in tiers)
                    {
                        var link = ParseLinkToFullDescription(tier);
                        var date = ParseDate(tier);

                        if (date > toDate)
                            headers.Add(new AdvertHeader()
                            {
                                DateSite = date,
                                Url = link,
                                Setting = url.ParserSetting
                            });
                    }
                }
                else
                {
                    Trace.TraceInformation("Can't find adverts");
                    reAtempt = true;
                }
            }
            while (headers.Count != oldCount && headers.Count < param.MaxCount);

            return headers;
        }

        private string ParseSeller(HtmlDocument full)
        {
            var seller = full.GetElementbyId("seller");
            if (seller != null)
            {
                var strong = seller.SelectSingleNode(@".//strong");
                if (strong != null)
                    return Normalize(strong.InnerText);
            }

            throw new ParsingException("Can't get seller", "");
        }

        private string ParseLinkToFullDescription(HtmlNode tier)
        {
            var link = tier.SelectSingleNode(@".//h3[@class='t_i_h3']/a");
            if (link != null && link.Attributes.Contains("href"))
                return "http://www.avito.ru" + Normalize(link.Attributes["href"].Value);

            throw new ParsingException("Can't get link to full description", "");
        }

        private DateTime ParseDate(HtmlNode tier)
        {
            var whenNode = tier.SelectSingleNode(@".//div[contains(@class,'t_i_date')]");
            if (whenNode != null && !string.IsNullOrEmpty(whenNode.InnerText))
            {
                var whenItems = new List<string>(whenNode.InnerText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));

                for (int i = 0; i < whenItems.Count; i++)
                    whenItems[i] = whenItems[i].Trim().ToLower();

                whenItems.RemoveAll(s => String.IsNullOrEmpty(s));

                if (whenItems.Count == 2)
                {
                    var dateString = whenItems[0];
                    var timeString = whenItems[1];

                    DateTime dt = DateTime.MinValue;

                    if (dateString == "сегодня")
                        dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    else if (dateString == "вчера")
                        dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1);
                    else
                    {
                        var dateS = dateString.Split(' ');
                        if (dateS != null && dateS.Count() == 2)
                        {
                            var month = 0;
                            switch (dateS[1])
                            {
                                case "янв.": month = 1; break;
                                case "февр.": month = 2; break;
                                case "марта": month = 3; break;
                                case "апр.": month = 4; break;
                                case "мая": month = 5; break;
                                case "июня": month = 6; break;
                                case "июля": month = 7; break;
                                case "авг.": month = 8; break;
                                case "сен.": month = 9; break;
                                case "окт.": month = 10; break;
                                case "нояб.": month = 11; break;
                                case "дек.": month = 12; break;
                                default:
                                    throw new ParsingException("Can't parse date information", dateString);
                            }

                            dt = new DateTime(DateTime.Now.Year, month, Int32.Parse(dateS[0]));
                            if (DateTime.Now.Month < month)
                                dt = dt.AddYears(-1);
                        }
                    }

                    var timeS = timeString.Split(':');
                    if (timeS != null && timeS.Count() == 2)
                    {
                        dt = dt.AddHours(Int32.Parse(timeS[0]));
                        dt = dt.AddMinutes(Int32.Parse(timeS[1]));

                        return dt;
                    }
                    else
                        throw new ParsingException("Can't parse date information", timeString);
                }
            }

            throw new ParsingException("Can't parse date information", "");
        }

        private void ParseTitle(HtmlDocument full, Advert advert)
        {
            var header = full.DocumentNode.SelectSingleNode(@".//h1[contains(@class,'item_title') or contains(@class,'item_title item_title-large')]");
            if (header != null)
            {
                var title = Normalize(header.InnerText);
                advert.Title = title;

                var parts = title.Split(',');
                if (parts.Length > 1)
                {
                    advert.Rooms = parts[0].Trim(new string[] { ">", "-к квартира" }).Trim();

                    if (parts.Length > 1)
                    {
                        float area;
                        float.TryParse(parts[1].Replace(" м²", "").Trim(), out area);
                        advert.AreaFull = area;
                    }

                    if (parts.Length > 2)
                    {
                        var floors = parts[2].Replace(" эт.", "").Trim().Split('/');

                        if (floors.Length > 0)
                        {
                            int floor;
                            Int32.TryParse(floors[0], out floor);
                            advert.Floor = (short)floor;
                        }

                        if (floors.Length > 1)
                        {
                            int floorfull;
                            Int32.TryParse(floors[1], out floorfull);
                            advert.FloorTotal = (short)floorfull;
                        }
                    }

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
            if (cityLabel != null)
            {
                var city = cityLabel.ChildNodes.Where(span => span.Attributes["class"] != null && span.Attributes["class"].Value == "c-1"); //todo check this
                if (city != null)
                    advert.City = Normalize(String.Join(", ", city.Select(c => c.InnerText))).TrimEnd(new[] { ',', ' ' });

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
            }
        }

        private void ParseAddress(HtmlDocument full, Advert advert)
        {
            var addressLabel = full.DocumentNode.SelectSingleNode(@"//div[@class='description_term']/span[text() = 'Адрес']");
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
                            advert.MetroStation = metro.InnerText.TrimEnd(new[] { ',' });
                            metro.Remove();
                        }

                        advert.Address = Normalize(addressBlock.InnerText).TrimEnd(new[] { ',' }).TrimStart(new[] { ',' }).Trim();
                    }
                }
            }

            if (String.IsNullOrEmpty(advert.Address))
            {
                var adress = full.DocumentNode.SelectSingleNode(@"//span[@itemprop='streetAddress']");
                if (adress != null)
                    advert.Address = adress.InnerText;
            }
        }

        private Int64 ParsePrice(HtmlDocument full)
        {
            var block = full.DocumentNode.SelectSingleNode(@".//span[contains(@class,'p_i_price t-item-price')]/strong");
            if (block != null)
            {
                var priceString = block.InnerText.Replace("&nbsp;", "").Replace(" руб.", "");
                if (priceString.Contains("Не указана"))
                    return 0;
                long price;
                if (Int64.TryParse(priceString, out price))
                    return price;
                else
                    throw new ParsingException("Can't parse price", block.InnerText);
            }

            throw new ParsingException("Can't find price", "");
        }

        private static AdvertType MapType(string param)
        {
            var dealMap = new Dictionary<string, AdvertType> {
                { "prodam", AdvertType.Sell },
                { "sdam", AdvertType.Pass }

            };

            return dealMap.ContainsKey(param) ? dealMap[param] : AdvertType.All;
        }

        private static Usedtype MapUsedtype(string param)
        {
            var subTypeMap = new Dictionary<string, Usedtype> {
                { "novostroyka", Usedtype.New }
            };

            return subTypeMap.ContainsKey(param) ? subTypeMap[param] : Usedtype.All;
        }

        private static RealEstateType MapRealEstateType(string param)
        {
            var typeMap = new Dictionary<string, RealEstateType> {
                { "kvartiry", RealEstateType.Apartments }
            };

            return typeMap.ContainsKey(param) ? typeMap[param] : RealEstateType.All;
        }

        private void ParseCategory(HtmlDocument full, Advert advert)
        {
            var nodes = full.DocumentNode.SelectNodes(@"//dl[@class='description description-expanded']/dd[@class='item-params c-1']/div[1]/a");
            if (nodes != null && nodes.Count > 0)
            {
                var node = nodes.Last();

                if (node.Attributes.Contains("href"))
                {
                    var href = node.Attributes["href"].Value;

                    var param = href.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var count = param.Count();
                    for (int i = 0; i < count; i++)
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

            }

        }

        private string ParseDescription(HtmlDocument full)
        {
            var desc = full.GetElementbyId("desc_text");
            if (desc != null)
                return Normalize(desc.InnerText);

            return "";
        }

        private List<Image> ParsePhotos(HtmlDocument full)
        {
            var result = new List<Image>();

            var gallery = full.DocumentNode.SelectSingleNode("//div[@class='gallery scrollable']/div[@class='items']");
            if (gallery != null)
            {
                foreach (var link in gallery.SelectNodes("//div[contains(@class, 'll fit')]/a"))
                {
                    if (result.Count >= Settings.SettingsStore.MaxCountOfImages)
                        break;

                    try
                    {
                        var href = link.Attributes["href"];
                        if (href != null && href.Value != null)
                        {
                            var src = Normalize(href.Value).TrimStart('/');

                            result.Add(new Image() { URl = "http://" + src });
                        }
                    }
                    catch (Exception)
                    {
                        if (result.Count > 0)
                            continue;

                        throw;
                    }
                }
            }

            return result;
        }

        private string ParsePhoneImageUrl(HtmlDocument full, string itemId)
        {
            var script = full.DocumentNode.SelectSingleNode("//div[@class='item']/script");
            if (script == null)
                return null;

            var urlRegex = new Regex(@"var item_url = '([^']+)").Match(script.InnerText);
            var itemRegex = new Regex(@"item_phone = '([^']+)").Match(script.InnerText);

            if (urlRegex.Success && itemRegex.Success)
            {
                var itemUrl = urlRegex.Groups[1].Value;
                var itemPhone = itemRegex.Groups[1].Value;

                var pre = new List<string>();
                foreach (Match item in new Regex("[0-9a-f]+", RegexOptions.IgnoreCase).Matches(itemPhone))
                    pre.Add(item.Value);

                if (Convert.ToInt64(itemId) % 2 == 0)
                    pre.Reverse();

                var mixed = string.Join("", pre.ToArray());
                var s = mixed.Length;
                var pkey = "";

                for (var k = 0; k < s; ++k)
                    if (k % 3 == 0)
                        pkey += mixed[k];

                return string.Format(@"http://www.avito.ru/items/phone/{0}?pkey={1}", itemUrl, pkey);
            }

            return null;
        }

        private void ParsePhone(HtmlDocument full, Advert advert, WebProxy proxy)
        {
            var itemId = Normalize(full.GetElementbyId("item_id").InnerText);

            var phoneUrl = ParsePhoneImageUrl(full, itemId);
            if (phoneUrl != null)
            {
                byte[] phoneImage = null;

                try
                {
                    phoneImage = DownloadImage(phoneUrl, UserAgents.GetRandomUserAgent(), null, CancellationToken.None, advert.Url, true);
                }
                catch (Exception)
                {
                    Trace.Write("Error during downloading image!");
                    throw;
                }

                var phoneText = OCRManager.RecognizeImage(phoneImage);

                //System.IO.File.WriteAllBytes(@"c:/test/" + phoneImage.GetHashCode() + ".png", phoneImage);

                advert.PhoneNumber = phoneText.ToString();
            }
        }

        public override Advert Parse(AdvertHeader header, WebProxy proxy, CancellationToken ct, PauseToken pt)
        {
            Advert advert = new Advert();

            try
            {
                advert.DateUpdate = DateTime.Now;

                advert.DateSite = header.DateSite;
                advert.Url = header.Url;
                advert.ImportSite = ImportSite.Avito;

                string result;
                result = this.DownloadPage(advert.Url, UserAgents.GetRandomUserAgent(), proxy, ct, true);
                if (result.Length < 200)
                    throw new BadResponseException();

                Console.WriteLine("Downloaded description");
                HtmlDocument page = new HtmlDocument();
                page.LoadHtml(result);

                ParseTitle(page, advert);

                advert.Name = ParseSeller(page);
                ParseCity(advert, page);
                ParseAddress(page, advert);

                ParseCategory(page, advert);

                advert.MessageFull = ParseDescription(page);

                advert.Price = ParsePrice(page);

                advert.Images = ParsePhotos(page);
                ParsePhone(page, advert, proxy);

                return advert;
            }
            catch (ParsingException)
            {
                Trace.WriteLine(advert.Url);
                throw;
            }
        }

    }
}
