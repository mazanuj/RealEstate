using HtmlAgilityPack;
using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using RealEstate.Proxies;
using RealEstate.ViewModels;
using RealEstate.OCRs;

namespace RealEstate.Parsing.Parsers
{
    public class HandsParser : ParserBase
    {
        public override List<AdvertHeader> LoadHeaders(ParserSourceUrl url, DateTime toDate, TaskParsingParams param, int maxAttemptCount, ProxyManager proxyManager, CancellationToken token)
        {
            List<AdvertHeader> headers = new List<AdvertHeader>();
            int oldCount = -1;
            int index = 0;
            int maxIndex = 10;

            do
            {
                oldCount = headers.Count;
                index++;

                string result = null;
                int attempt = 0;
                string uri = "";

                while (attempt < maxAttemptCount)
                {
                    attempt++;
                    token.ThrowIfCancellationRequested();
                    WebProxy proxy = /*param.useProxy  ? proxyManager.GetNextProxy() :*/ null;
                    try
                    {
                        uri = url.Url + ((index != 1) ? ("page" + index) : "") + "/";
                        var referer = url.Url + ((index - 1 != 1) ? ("page" + (index - 1)) : "");
                        Trace.WriteLine("Downloading " + uri);

                        result = this.DownloadPage(uri, UserAgents.GetRandomUserAgent(), proxy, CancellationToken.None, true);
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

                //if (page.DocumentNode.SelectSingleNode(@"//div[contains(@class,'adds_cont clear')]") != null)
                //    break;

                var tiers = page.DocumentNode.SelectNodes(@"//div[@data-position and @data-item-id]");
                if (tiers == null)
                {
                    Trace.TraceInformation(url.Url);
                    throw new ParsingException("Can't find headers adverts", "");
                }

                if (index == 1)
                {
                    maxIndex = GetMaxIndex(page);
                }

                //Trace.TraceInformation("--------URLs-------------");
                //Trace.TraceInformation("!! " + uri);                

                foreach (HtmlNode tier in tiers)
                {
                    var link = ParseLinkToFullDescription(tier);
                    var date = ParseDate(tier);

                    if (date > toDate)
                    {
                        //Trace.TraceInformation(link);
                        headers.Add(new AdvertHeader()
                        {
                            DateSite = date,
                            Url = link,
                            Setting = url.ParserSetting
                        });
                    }
                }

                //Trace.TraceInformation("----------------------");
            }
            while (headers.Count != oldCount && headers.Count < param.MaxCount && index <= maxIndex);
            return headers;
        }

        private int GetMaxIndex(HtmlDocument page)
        {
            var maxNode = page.DocumentNode.SelectSingleNode(@"//ul[contains(@class,'same_adds_paging')]/li[last()]");
            if (maxNode != null)
            {
                int max = 0;
                if (Int32.TryParse(maxNode.InnerText, out max))
                    return max;
                else
                    throw new ParsingException("can't parse max index!", maxNode.InnerText);
            }
            else
                throw new ParsingException("can't find max index!", "");
        }

        private DateTime ParseDate(HtmlNode tier)
        {
            var whenNode = tier.SelectSingleNode(@".//p[contains(@class,'adv_data')]");
            if (whenNode != null && !string.IsNullOrEmpty(whenNode.InnerText))
            {
                try
                {
                    return DateTime.ParseExact(whenNode.InnerText, "HH:mm, dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                }
                catch (Exception)
                {
                    throw new ParsingException("Can't parse date information", whenNode.InnerText);
                }
            }

            throw new ParsingException("Can't parse date information", "");
        }

        private string ParseLinkToFullDescription(HtmlNode tier)
        {
            var link = tier.SelectSingleNode(".//a[contains(@class,\"add_title\")]");
            if (link != null && link.Attributes.Contains("href"))
                return Normalize(link.Attributes["href"].Value);

            throw new ParsingException("Can't get link to full description", "");
        }

        public override Advert Parse(AdvertHeader header, System.Net.WebProxy proxy, CancellationToken ct, PauseToken pt, bool onlyPhone = false)
        {
            Advert advert = new Advert();
            proxy = null;
            try
            {

                advert.DateUpdate = DateTime.Now;

                advert.DateSite = header.DateSite;
                advert.Url = header.Url;
                advert.ImportSite = ImportSite.Hands;
                advert.AdvertType = AdvertType.Sell;

                string result;
                result = this.DownloadPage(advert.Url, UserAgents.GetRandomUserAgent(), proxy, ct);
                if (result.Length < 200)
                    throw new BadResponseException();

                HtmlDocument page = new HtmlDocument();
                page.LoadHtml(result);

                if (!onlyPhone)
                {
                    ParseCategory(advert);

                    ParseTitle(page, advert);
                    ParsePrice(page, advert);

                    advert.Name = ParseSeller(page);
                    ParseAddress(page, advert);

                    advert.MetroStation = ParseMetro(page);

                    ParseProperties(page, advert);

                    ParseDescription(page, advert);

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

        private string ParseMetro(HtmlDocument page)
        {
            var metroNode = page.DocumentNode.SelectSingleNode(@"//div[contains(@class,'clear')]/div/p/span[contains(@class,'metro')]");
            if (metroNode != null)
                return metroNode.InnerText;

            return null;
        }

        private void ParseCategory(Advert advert)
        {
            var parts = advert.Url.Replace("http://", "").Split(new char[] { '/' });
            foreach (var part in parts)
            {
                switch (part)
                {
                    case "apartments-sale":
                        advert.RealEstateType = RealEstateType.Apartments;
                        advert.AdvertType = AdvertType.Sell;
                        break;
                    case "new":
                        advert.Usedtype = Usedtype.New;
                        break;
                    case "secondary":
                        advert.Usedtype = Usedtype.Used;
                        break;
                    default:
                        break;
                }
            }
        }

        private void ParseProperties(HtmlDocument page, Advert advert)
        {
            var detailsNodes = page.DocumentNode.SelectNodes("//ul[contains(@class,'form_info form_info_short') or contains(@class,'form_info')]");
            if (detailsNodes != null)
            {
                foreach (var detailsNode in detailsNodes)
                {
                    foreach (var detail in detailsNode.SelectNodes("li"))
                    {
                        var parts = detail.InnerText.Trim().Split(new char[] { ':' });
                        try
                        {
                            switch (parts[0])
                            {
                                case "Этаж":
                                    advert.Floor = Int16.Parse(parts[1].Trim());
                                    break;
                                case "Комнат в квартире":
                                    advert.Rooms = parts[1].Trim();
                                    break;
                                case "Общая площадь":
                                    advert.AreaFull = float.Parse(parts[1].Replace(" кв.м", "").Trim(), CultureInfo.InvariantCulture);
                                    break;
                                case "Площадь кухни":
                                    advert.AreaKitchen = float.Parse(parts[1].Replace(" кв.м", "").Trim(), CultureInfo.InvariantCulture);
                                    break;
                                case "Жилая площадь":
                                    advert.AreaLiving = float.Parse(parts[1].Replace(" кв.м", "").Trim(), CultureInfo.InvariantCulture);
                                    break;
                                case "Этажей в здании":
                                    advert.FloorTotal = Int16.Parse(parts[1].Trim());
                                    break;
                                case "Район города":
                                    advert.Distinct = parts[1].Trim();
                                    break;
                                case "АО":
                                    advert.AO = parts[1].Replace("административный округ", "").Trim();
                                    break;
                                case "Продавец":
                                    advert.Name = GetSeller(parts[1]);
                                    break;
                                case "Контактное лицо":
                                    advert.Name = GetSeller(parts[1]);
                                    break;
                                case "Год постройки/сдачи":
                                    advert.BuildingYear = Regex.Match(parts[1], @"20(?<year>\d{2})", RegexOptions.IgnoreCase).Groups["year"].Value;
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            Trace.WriteLine("Unrecoginzed data: " + parts[1], "Parsing error");
                        }
                    }
                }
            }
            else
                throw new ParsingException("Can't find details", "");
        }

        private ICollection<Image> ParsePhotos(HtmlDocument page)
        {
            var result = new List<Image>();

            var gallery = page.DocumentNode.SelectSingleNode("//ul[@class='slider_pagination overview']");
            if (gallery != null)
            {
                foreach (var link in gallery.SelectNodes("li/a/img"))
                {
                    if (result.Count >= Settings.SettingsStore.MaxCountOfImages)
                        break;

                    try
                    {
                        var href = link.Attributes["src"];
                        if (href != null && href.Value != null)
                        {
                            var src = Normalize(href.Value).Replace("-small", "-view");

                            result.Add(new Image() { URl = src });
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

        private void ParsePhone(HtmlDocument page, Advert advert, WebProxy proxy)
        {
            var phoneNode = page.DocumentNode.SelectSingleNode(".//input[contains(@id,'allphones') and contains(@type,'hidden')]");
            if (phoneNode != null)
            {
                var sellerPhone = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(phoneNode.GetAttributeValue("value", ""))).Trim();
                Regex r = new Regex(@"'(.+jpg)'");
                sellerPhone = r.Match(sellerPhone).Groups[0].Value;
                if (sellerPhone == "") return;

                byte[] phoneImage = null;

                try
                {
                    phoneImage = DownloadImage(sellerPhone.Trim(new char[] { '\'' }), UserAgents.GetRandomUserAgent(), proxy, CancellationToken.None, Normalize(advert.Url), true);
                }
                catch (Exception ex)
                {
                    Trace.Write("Error when downloading image", "Web error!");
                    throw;
                }

                advert.PhoneNumber = OCRManager.RecognizeImage(phoneImage);
            }
        }

        private void ParseDescription(HtmlDocument page, Advert advert)
        {
            var textNode = page.DocumentNode.SelectSingleNode(".//p[contains(@class,'text')]");
            if (textNode != null)
            {
                advert.MessageFull = Normalize(textNode.InnerText);

                var parts = advert.MessageFull.Split(new char[] { '\n' });
                var phoneStr = parts.LastOrDefault(s => s.ToLower().Contains("тел.") || s.Contains("т."));
                if (phoneStr != null)
                    advert.PhoneNumber = phoneStr.ToLower().Trim(new string[] { "тел.: ", "т.", ":" }).Trim();
            }
            else
                throw new ParsingException("Can't get description", "");

        }

        private void ParseAddress(HtmlDocument page, Advert advert)
        {
            var adressNode = page.DocumentNode.SelectSingleNode(".//a[contains(@class,'address_link')]");
            if (adressNode != null)
            {
                var adress = adressNode.InnerText;
                var parts = adress.Split(new char[] { ',' });

                advert.City = parts[0];
                advert.Address = String.Join(",", parts.Skip(1).ToArray());
            }
        }

        private string ParseSeller(HtmlDocument page)
        {
            var phoneNode = page.DocumentNode.SelectSingleNode(".//ul[contains(@class,'form_info')]/li/p[2]");
            if (phoneNode != null)
            {
                return GetSeller(phoneNode.InnerText);
            }
            return string.Empty;
        }

        private string GetSeller(string trash)
        {
            var i = trash.IndexOf("—");
            if (i != -1)
                trash = trash.Substring(0, i);
            return trash.Trim(new string[] { "X", "-", "Показать телефон", "(", ")", "+", "—", ",", ";", "\r\n" }).Trim();
        }

        private void ParseTitle(HtmlDocument page, Advert advert)
        {
            var titleNode = page.DocumentNode.SelectSingleNode("//table/tr/td/h1");
            if (titleNode != null)
            {
                var title = titleNode.InnerText;
                advert.Title = Normalize(title).Trim();
            }
            else
                throw new ParsingException("none header", "");

        }

        private void ParsePrice(HtmlDocument page, Advert advert)
        {
            var priceNode = page.DocumentNode.SelectSingleNode(".//div[contains(@class,'credit_cost')]");
            priceNode.SelectSingleNode("u").RemoveAllChildren();
            if (priceNode != null)
            {
                var price = Normalize(priceNode.InnerText);
                int pr;
                Int32.TryParse(price.Replace(" руб.", "").Replace(".", "").Trim(), out pr);
                advert.Price = pr;
            }
            else
                throw new ParsingException("none price", "");
        }

        public override int GetTotalCount(string sourceUrl, ProxyManager proxyManager, bool useProxy, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
