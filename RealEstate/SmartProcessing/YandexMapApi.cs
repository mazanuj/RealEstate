using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RealEstate.SmartProcessing
{
    public class YandexMapApi
    {
        public string SearchObject(string Address, out string city)
        {
            var urlXml = "http://geocode-maps.yandex.ru/1.x/?geocode=" + Address + "&results=5";
            var client = new WebClient();
            client.Encoding = new UTF8Encoding(false);
            var source = client.DownloadString(urlXml);


            city = null;
            var reg = new Regex(@"<LocalityName>(.+)</LocalityName>");
            var cityMatch = reg.Match(source);
            if (cityMatch.Success && cityMatch.Groups.Count > 1)
                city = cityMatch.Groups[1].Value;

            if (!String.IsNullOrEmpty(city))
                city = city.Replace("поселок городского типа", "").Trim();

            var r = new Regex("<name xmlns=\"http://www.opengis.net/gml\">(.+)</name>");
            var m = r.Match(source);
            if(m.Success && m.Groups.Count > 1)
            {
                var result = m.Groups[1].Value;;
                if (!String.IsNullOrEmpty(result))
                    result = city.Replace("поселок городского типа", "").Trim();
                return result;
            }

            return null;
        }
    }
}
