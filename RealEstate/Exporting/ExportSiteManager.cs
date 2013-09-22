using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Db;
using RealEstate.Parsing;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportSiteManager))]
    public class ExportSiteManager
    {
        private readonly RealEstateContext context = null;
        public BindableCollection<ExportSite> ExportSites = null;

        [ImportingConstructor]
        public ExportSiteManager(RealEstateContext context)
        {
            this.context = context;
        }

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
            ExportSites = new BindableCollection<ExportSite>(context.ExportSites.OrderBy(e => e.ParseSettings.Count));
        }

    }
}
