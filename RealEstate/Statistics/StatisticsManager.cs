using RealEstate.Parsing;
using RealEstate.Parsing.Parsers;
using RealEstate.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace RealEstate.Statistics
{
    [Export(typeof(StatisticsManager))]
    public class StatisticsManager
    {
        public string BuildUrl(ImportSite site, string city, RealEstateType rType, AdvertType aType, Usedtype uType)
        {
            switch (site)
            {
                case ImportSite.Avito:
                    return "http://www.avito.ru/" + city + "/" + AvitoParser.MapRealEstateType(rType) 
                        + "/" + AvitoParser.MapType(aType) + (uType != Usedtype.All  ? ("/" + AvitoParser.MapUsedType(uType)) : "");
                case ImportSite.Hands:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        public void Save(List<StatViewItem> items, string FileName)
        {
            if (!File.Exists(FileName))
            {
                var str = File.CreateText(FileName);
                str.Close();
            }

            var writer = new XmlSerializer(typeof(List<StatViewItem>));
            StreamWriter file = new StreamWriter(FileName);
            writer.Serialize(file, items);
            file.Close();
        }

        public List<StatViewItem> Restore(string FileName)
        {
            try
            {
                if (File.Exists(FileName))
                {
                    XmlSerializer reader = new XmlSerializer(typeof(List<StatViewItem>));
                    StreamReader file = new StreamReader(FileName);
                    return (List<StatViewItem>)reader.Deserialize(file);
                }

                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                return null;
            }
        }
    }

    public class StatisticItem
    {
        public string Url { get; set; }
        public string City { get; set; }
        public string CityKey { get; set; }
        public ImportSite Site { get; set; }
        public RealEstateType rType { get; set; } 
        public AdvertType aType { get; set; } 
        public Usedtype uType { get; set; }
    }
}
