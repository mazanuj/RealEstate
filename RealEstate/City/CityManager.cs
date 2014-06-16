using System.Collections.Generic;
using System.Linq;
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
        public BindableCollection<CityWrap> NotSelectedCities = new BindableCollection<CityWrap>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<CityWrap>));
                StreamReader file = new StreamReader(FileName);
                var list = (List<CityWrap>)reader.Deserialize(file);
                Cities.Add(new CityWrap() { City = "Все", IsSelected = true });
                Cities.AddRange(list.Where(c => c.IsSelected));
                NotSelectedCities.AddRange(list.Where(c => c.City != CityWrap.ALL));
            }
            else
            {
                RestoreDefaults();
            }
        }

        private void RestoreDefaults()
        {
            Trace.WriteLine("Restore default cities");

            Cities.Add(new CityWrap() { City = "Все", IsSelected = true });

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
            StreamWriter file = new StreamWriter(FileName);
            writer.Serialize(file, NotSelectedCities.ToList());
            file.Close();
        }
    }

    public class CityWrap : PropertyChangedBase
    {
        public const string ALL = "Все";

        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            set
            {
                _IsActive = value;
                NotifyOfPropertyChange(() => IsActive);
            }
        }

        public string City { get; set; }
        public string AvitoKey { get; set; }
        public string HandsKey { get; set; }
        public bool IsSelected { get; set; }
        public string Parent { get; set; }
    }
}
