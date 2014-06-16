﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RealEstate.OKATO
{
    public class KLADRDriver
    {
        public string GetOKATO(string kladr)
        {
            WebClient client = new WebClient();
            var source = client.DownloadString(@"http://www.alta.ru/kladrs/search/?type=&object_rf=&code=" + kladr + "&ocatd=&idx=&SearchButton=%CD%E0%E9%F2%E8");

            Regex r = new Regex("<a href=\"/kladrs/object/path/dom/(\\d+)");
            var m = r.Match(source);
            if(m.Success && m.Groups.Count > 0)
            {
                var url = "http://www.alta.ru/kladrs/object/path/dom/" + m.Groups[1].Value;

                source = client.DownloadString(url);

                r = new Regex("<td><nobr>(\\d+)</nobr></td>");
                m = r.Matches(source).Cast<Match>().Last();
                if (m.Success && m.Groups.Count > 0)
                {
                    return m.Groups[1].Value;
                }
            }

             return null;

        }
    }
}
