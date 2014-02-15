using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace RealEstate.Exporting
{
    [Export(typeof(PhonesManager))]
    public class PhonesManager
    {
        private const string FileName = "phones.xml";

        public List<PhoneCollection> PhoneCollections = new List<PhoneCollection>();

        public void Restore()
        {
            if (File.Exists(FileName))
            {
                XmlSerializer reader = new XmlSerializer(typeof(List<PhoneCollection>));
                StreamReader file = new System.IO.StreamReader(FileName);
                PhoneCollections.AddRange((List<PhoneCollection>)reader.Deserialize(file));
            }
            else
            {
                RestoreDefaults();
            }
        }

        private void RestoreDefaults()
        {
            Trace.WriteLine("Restore default phones");
            Save();
        }

        public void Save()
        {
            if (!File.Exists(FileName))
            {
                var str = File.CreateText(FileName);
                str.Close();
            }

            var writer = new XmlSerializer(typeof(List<PhoneCollection>));
            StreamWriter file = new System.IO.StreamWriter(FileName);
            writer.Serialize(file, PhoneCollections.ToList());
            file.Close();
        }

        public string GetRandomPhone(int siteId)
        {
            var col = PhoneCollections.FirstOrDefault(s => s.SiteId == siteId);
            if (col == null || col.Numbers == null || col.Numbers.Count == 0)
                return null;
            if (col.Numbers.Count == 1)
                return col.Numbers[0];

            int max = col.Numbers.Count;
            int i = new Random(DateTime.Now.Millisecond).Next(1, max);
            return col.Numbers[i - 1];
        }
    }

    public class PhoneCollection
    {
        public List<string> Numbers { get; set; }
        public int SiteId { get; set; }
    }
}
