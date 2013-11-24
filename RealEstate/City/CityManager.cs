using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Caliburn.Micro;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Xml.Serialization;

namespace RealEstate.City
{
    [Export(typeof(CityManager))]
    public class CityManager
    {
        private const string FileName = "City\\cities.xml";
        public BindableCollection<CityWrap> Cities = new BindableCollection<CityWrap>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<CityWrap>));
                StreamReader file = new System.IO.StreamReader(FileName);
                Cities.AddRange((List<CityWrap>)reader.Deserialize(file));
            }
            else
            {
                RestoreDefaults();
            }
        }

        private void RestoreDefaults()
        {
            Trace.WriteLine("Restore default cities");
            Cities.Add(new CityWrap() { City = "" });
            Cities.Add(new CityWrap() { City = "Ярославль", AvitoKey = "yaroslavl" });

            Save();
        }

        public void Save()
        {
            if (!File.Exists(FileName))
            {
                var str = File.CreateText(FileName);
                str.Close();
            }

            var writer = new XmlSerializer(typeof(List<CityWrap>));
            StreamWriter file = new System.IO.StreamWriter(FileName);
            writer.Serialize(file, Cities.ToList());
            file.Close();
        }
    }

    public class CityWrap
    {
        public string City { get; set; }
        public string AvitoKey { get; set; }
        public string HandsKey { get; set; }
    }
}
