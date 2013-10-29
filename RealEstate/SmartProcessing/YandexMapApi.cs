using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RealEstate.SmartProcessing
{
    public class YandexMapApi
    {
        public string SearchObject(string Address)
        {
            string urlXml = "http://geocode-maps.yandex.ru/1.x/?geocode=" + Address + "&results=1";
            WebClient client = new WebClient();
            client.Encoding = new System.Text.UTF8Encoding(false);
            var source = client.DownloadString(urlXml);
            Regex r = new Regex("<name xmlns=\"http://www.opengis.net/gml\">(.+)</name>");
            var m = r.Match(source);
            if(m.Success && m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return null;
        }
    }
}
