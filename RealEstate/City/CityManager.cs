using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Caliburn.Micro;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace RealEstate.City
{
    [Export(typeof(CityManager))]
    public class CityManager
    {
        private const string FileName = "cities.txt";
        public BindableCollection<CityWrap> Cities = new BindableCollection<CityWrap>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                var cities = File.ReadAllLines(FileName);
                Cities.AddRange(cities.Select(c => new CityWrap(){City = c}));
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
            Cities.Add(new CityWrap() { City = "Ярославль" });

            Save();
        }

        public void Save()
        {
            if (!File.Exists(FileName))
            {
                var str = File.CreateText(FileName);
                str.Close();
            }

            if (Cities.Count != 0)
            {
                File.WriteAllLines(FileName, Cities.Select(c => c.City).ToArray());
            }
        }
    }

    public class CityWrap
    {
        public string City { get; set; }
    }
}
