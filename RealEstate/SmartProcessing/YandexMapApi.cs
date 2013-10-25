using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace RealEstate.SmartProcessing
{
    public class YandexMapApi
    {
        public string SearchObject(string Address)
        {
            string urlXml = "http://geocode-maps.yandex.ru/1.x/?geocode=" + Address + "&results=1";
            WebClient client = new WebClient();
            return client.DownloadString(urlXml);
        }
    }
}
