using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportingManager))]
    public class ExportingManager
    {
        private readonly RealEstateContext _context = null;

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context)
        {
            _context = context;
        }


        public IEnumerable<Advert> Filter(IEnumerable<Advert> adverts, ExportStatus status)
        {
            switch (status)
            {
                case ExportStatus.Unprocessed:
                    return adverts.Where(a => !_context.ExportItems.Any(e => e.AdvertId == a.Id));
                case ExportStatus.Exporting:
                    return adverts.Where(a => _context.ExportItems.Any(e => !e.IsExported && (e.AdvertId == a.Id)));
                case ExportStatus.Exported:
                    return adverts.Where(a => _context.ExportItems.Any(e => e.IsExported && (e.AdvertId == a.Id)));
                default:
                    return null;
            }
        }

        private Queue<ExportItem> ExportQueue = null;

        public void ResoreQueue()
        {
            ExportQueue = new Queue<ExportItem>(_context.ExportItems.Where(i => !i.IsExported));  
        }

        public void AddAdvertToExport(Advert advert)
        {
            var item = new ExportItem() { AdvertId = advert.Id };
            _context.ExportItems.Add(item);
            _context.SaveChanges();
        }

    }

    public enum ExportStatus
    {
        Unprocessed,
        Exporting,
        Exported
    }

    public class ExportItem
    {
        public int Id { get; set; }
        public bool IsExported { get; set; }
        public int AdvertId { get; set; }
        public DateTime DateOfExport { get; set; }
    }
}
