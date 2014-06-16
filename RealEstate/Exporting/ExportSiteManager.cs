using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using RealEstate.Db;

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

        public void Save(ExportSite site)
        {
            if (context != null)
            {
                if (context.ExportSites.Any(e => e.Id == site.Id))
                {
                    context.SaveChanges();
                }
                else
                {
                    ExportSites.Add(site);
                    context.ExportSites.Add(site);
                    context.SaveChanges();
                }
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
