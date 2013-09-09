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
        public BindableCollection<string> Cities  = new BindableCollection<string>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                Cities.AddRange(File.ReadAllLines(FileName));
            }
        }

        public void Save()
        {
            if (!File.Exists(FileName))
                File.CreateText(FileName);

            if (Cities.Count != 0)
            {
                File.WriteAllLines(FileName, Cities.ToArray());
            }
        }
    }
}
