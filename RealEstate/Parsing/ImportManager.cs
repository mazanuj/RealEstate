using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Caliburn.Micro;

namespace RealEstate.Parsing
{
    [Export(typeof(ImportManager))]
    public class ImportManager
    {
        private const string FileName = "imports.xml";
        public BindableCollection<ParsingSite> ParsingSites = new BindableCollection<ParsingSite>();

        public void Restore()
        {
            try
            {
                if (File.Exists(FileName))
                {
                    RestoreFromFile();
                }
                else
                {
                    RestoreDefaults();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                RestoreDefaults();
            }
        }

        private void RestoreDefaults()
        {
            Trace.WriteLine("Restore import sites settings to default");
            ParsingSites.Clear();
            ParsingSites.Add(new ParsingSite() { Site = ImportSite.All, DisplayName = GetSiteName(ImportSite.All), Deep = 200, Delay = 20 });
            ParsingSites.Add(new ParsingSite() { Site = ImportSite.Avito, DisplayName = GetSiteName(ImportSite.Avito), Deep = 200, Delay = 20 });
            ParsingSites.Add(new ParsingSite() { Site = ImportSite.Hands, DisplayName = GetSiteName(ImportSite.Hands), Deep = 200, Delay = 5 });

            Save();

        }

        private void RestoreFromFile()
        {
            using (XmlReader reader = XmlReader.Create(FileName))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<ParsingSite>), new XmlRootAttribute("sites"));
                ParsingSites.AddRange((List<ParsingSite>)ser.Deserialize(reader));
            }
        }

        public void Save()
        {
            using (XmlWriter writer = XmlWriter.Create(FileName))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<ParsingSite>), new XmlRootAttribute("sites"));
                ser.Serialize(writer, ParsingSites.ToList());
            }
        }

        public string GetSiteName(ImportSite site)
        {
            switch (site)
            {
                case ImportSite.All:
                    return "Все";
                case ImportSite.Avito:
                    return "avito.ru";
                case ImportSite.Hands:
                    return "irr.ru";
                default:
                    return "";
            }
        }
    }

    public class ParsingSite
    {
        public ImportSite Site { get; set; }
        public string DisplayName { get; set; }
        public int Delay { get; set; }
        public int Deep { get; set; }
    }

    public enum ImportSite
    {
        All,
        Avito,
        Hands
    }
}
