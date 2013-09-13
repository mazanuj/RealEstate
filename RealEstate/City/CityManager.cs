using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Caliburn.Micro;
using System.ComponentModel.Composition;

namespace RealEstate.City
{
    [Export(typeof(CityManager))]
    public class CityManager
    {
        private const string FileName = "cities.txt";
        public BindableCollection<CityManagerSelectable> Cities = new BindableCollection<CityManagerSelectable>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                var cities = File.ReadAllLines(FileName);
                Cities.AddRange(cities.Select(c => new CityManagerSelectable(){City = c, IsSelected = false}));
            }
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

    public class CityManagerSelectable
    {
        public string City { get; set; }
        public bool IsSelected { get; set; }
    }
}
