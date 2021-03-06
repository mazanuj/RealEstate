﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Caliburn.Micro;
using System.ComponentModel.DataAnnotations;
using RealEstate.Modes;

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
            using (var reader = XmlReader.Create(FileName))
            {
                var ser = new XmlSerializer(typeof(List<ParsingSite>), new XmlRootAttribute("sites"));
                ParsingSites.AddRange(((List<ParsingSite>)ser.Deserialize(reader)).Where(s => s.Site == ModeManager.SiteMode || ModeManager.SiteMode == ImportSite.All));
            }
        }

        public void Save()
        {
            using (var writer = XmlWriter.Create(FileName))
            {
                var ser = new XmlSerializer(typeof(List<ParsingSite>), new XmlRootAttribute("sites"));
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
        [Display(Name="Любой")]
        All,
        [Display(Name = "avito.ru")]
        Avito,
        [Display(Name = "Из рук в руки")]
        Hands
    }
}
