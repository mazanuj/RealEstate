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

namespace RealEstate.Parsing.Parsers
{
    public class HandsParser : ParserBase
    {

        public override List<AdvertHeader> LoadHeaders(ParserSourceUrl url, WebProxy proxy, DateTime toDate, int maxCount, int maxAttemptCount, ProxyManager proxyManager)
        {
            List<AdvertHeader> headers = new List<AdvertHeader>();
            int oldCount = -1;
            int index = 0;

            do
            {
                oldCount = headers.Count;
                index++;

                string result = null;
                int attempt = 0;

                while (attempt++ < maxAttemptCount)
                {
                    try
                    {
                        string uri = url.Url + "page" + index;
                        Trace.WriteLine("Downloading " + uri);
                        result = this.DownloadPage(uri, UserAgents.GetDefaultUserAgent(), proxy, CancellationToken.None);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Web Error!");
                        proxyManager.RejectProxy(proxy);
                    }
                }

                if (result == null) throw new Exception("Can't load headers adverts");

                HtmlDocument page = new HtmlDocument();
                page.LoadHtml(result);

                //if (page.DocumentNode.SelectSingleNode(@"//div[contains(@class,'adds_cont clear')]") != null)
                //    break;

                foreach (HtmlNode tier in page.DocumentNode.SelectNodes(@"//div[@data-position and @data-item-id]"))
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
            while (headers.Count != oldCount && headers.Count < maxCount);

            return headers;
        }

        private DateTime ParseDate(HtmlNode tier)
        {
            var whenNode = tier.SelectSingleNode(@"p[contains(@class,'add_data')]");
            if (whenNode != null && !string.IsNullOrEmpty(whenNode.InnerText))
            {
                try
                {
                    return DateTime.ParseExact(whenNode.InnerText, "HH:mm, dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                }
                catch (Exception)
                {
                    throw new ParsingException("Can't parse date information", "");
                }
            }

            throw new ParsingException("Can't parse date information", "");
        }

        private string ParseLinkToFullDescription(HtmlNode tier)
        {
            var link = tier.SelectSingleNode(@"a[@class='add_pic']");
            if (link != null && link.Attributes.Contains("href"))
                return Normalize(link.Attributes["href"].Value);

            throw new ParsingException("Can't get link to full description", "");
        }

        public override Advert Parse(AdvertHeader header, System.Net.WebProxy proxy, CancellationToken ct, PauseToken pt)
        {
            Advert advert = new Advert();

            advert.DateUpdate = DateTime.Now;

            advert.DateSite = header.DateSite;
            advert.Url = header.Url;
            advert.ImportSite = ImportSite.Hands;

            string result;
            result = this.DownloadPage(advert.Url, UserAgents.GetDefaultUserAgent(), proxy, ct);

            HtmlDocument page = new HtmlDocument();
            page.LoadHtml(result);

            try
            {
                ParseCategory(advert);
                ParseTitle(page, advert);
                ParsePrice(page, advert);

                advert.Name = ParseSeller(page);
                ParseAddress(page, advert);

                ParseProperties(page, advert);

                ParseDescription(page, advert);

                advert.Images = ParsePhotos(page);
                ParsePhone(page, advert);

                return advert;
            }
            catch (Exception)
            {
                Trace.WriteLine(advert.Url);
                throw;
            }
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
            var detailsNodes = page.DocumentNode.SelectNodes("//ul[contains(@class,'form_info form_info_short')]");
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
                    if (result.Count > 5)
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

        private void ParsePhone(HtmlDocument page, Advert advert)
        {
            var phoneNode = page.DocumentNode.SelectSingleNode(".//input[contains(@id,'allphones') and contains(@type,'hidden')]");
            if (phoneNode != null)
            {
                var sellerPhone = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(phoneNode.GetAttributeValue("value", "")));
                if (advert.Name != string.Empty)
                    advert.PhoneNumber = sellerPhone.Replace(advert.Name, "").Trim();
                else
                    advert.PhoneNumber = sellerPhone.Trim();
            }
        }

        private void ParseDescription(HtmlDocument page, Advert advert)
        {
            var textNode = page.DocumentNode.SelectSingleNode(".//p[contains(@class,'text')]");
            if (textNode != null)
            {
                advert.MessageFull = textNode.InnerText;

                var parts = advert.MessageFull.Split(new char[] { '\n' });
                var phoneStr = parts.LastOrDefault(s => s.ToLower().Contains("тел.") || s.Contains("т."));
                if (phoneStr != null)
                    advert.PhoneNumber = phoneStr.ToLower().Trim(new string[]{"тел.: ", "т.", ":"}).Trim();
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
            var phoneNode = page.DocumentNode.SelectSingleNode(".//p[contains(@id,'contact_phones')]");
            if (phoneNode != null)
            {
                var sellerPhone = phoneNode.InnerText;
                var name = sellerPhone.Trim(new string[] { "X", "-", "Показать телефон", "(", ")", "+", " — ", "," }).Trim();
                return name;

            }
            return string.Empty;
        }

        private void ParseTitle(HtmlDocument page, Advert advert)
        {
            var titleNode = page.DocumentNode.SelectSingleNode("//table/tr/td/h1");
            if (titleNode != null)
            {
                var title = titleNode.InnerText;
                advert.Title = title;
            }
            else
                throw new ParsingException("none header", "");

        }

        private static void ParsePrice(HtmlDocument page, Advert advert)
        {
            var priceNode = page.DocumentNode.SelectSingleNode(".//div[contains(@class,'credit_cost')]");
            if (priceNode != null)
            {
                var price = priceNode.InnerText;
                int pr = -1;
                Int32.TryParse(price.Replace(" руб.", "").Replace(".", "").Trim(), out pr);
                advert.Price = pr;
            }
            else
                throw new ParsingException("none price", "");
        }
    }
}
