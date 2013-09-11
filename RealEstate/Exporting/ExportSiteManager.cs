using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Db;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportSiteManager))]
    public class ExportSiteManager
    {
        private RealEstateContext context = null;
        public BindableCollection<ExportSite> ExportSites = null;

        public void Add(ExportSite site)
        {
            if (context != null)
            {
                ExportSites.Add(site);
                context.ExportSites.Add(site);
                context.SaveChanges();
            }
        }

        public void Delete(ExportSite site)
        {
            if (context != null)
            {
                context.ExportSites.Remove(site);
                ExportSites.Remove(site);
                context.SaveChanges();
            }
        }

        public void Restore()
        {
            context = new RealEstateContext();
            ExportSites = new BindableCollection<ExportSite>(context.ExportSites.OrderBy(e => e.ParseSettings.Count));
        }
    }
}
