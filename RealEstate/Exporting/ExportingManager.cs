using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
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
                    return adverts.Where(a => !_context.ExportItems.Any(e => e.Advert.Id == a.Id));
                case ExportStatus.Exporting:
                    return adverts.Where(a => _context.ExportItems.Any(e => !e.IsExported && (e.Advert.Id == a.Id)));
                case ExportStatus.Exported:
                    return adverts.Where(a => _context.ExportItems.Any(e => e.IsExported && (e.Advert.Id == a.Id)));
                default:
                    return null;
            }
        }

        public ObservableCollection<ExportItem> ExportQueue = null;

        public void RestoreQueue()
        {
            ExportQueue = new ObservableCollection<ExportItem>();
            foreach (var item in _context.ExportItems.Where(i => !i.IsExported))
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Add(item);
                }));
            }
        }

        public void AddAdvertToExport(Advert advert)
        {
            var item = new ExportItem() { Advert = advert };
            _context.ExportItems.Add(item);
            _context.SaveChanges();
            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ExportQueue.Add(item);
            }));
        }

        public void Remove(ExportItem item)
        {
            App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Remove(item);
                }));
            _context.ExportItems.Remove(item);

            _context.SaveChanges();
        }

        public void Export(ExportItem item)
        {
            if (item.Advert.ExportSites != null)
                foreach (var site in item.Advert.ExportSites)
                {
                    var settings = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == site.Id);
                    if (settings != null)
                    {
                        if (settings.UsedtypeValue != item.Advert.UsedtypeValue ||
                            settings.RealEstateTypeValue != item.Advert.RealEstateTypeValue ||
                            settings.AdvertTypeValue != item.Advert.AdvertTypeValue)
                            continue;
                    }

                    ExportAdvert(item.Advert);

                }

            item.DateOfExport = DateTime.Now;
            item.IsExported = true;
            _context.SaveChanges();
        }

        private void ExportAdvert(Advert advert)
        {
            var cstr = GetConnectionString(advert);
            if (cstr != null)
            {
                using (SqlConnection conn = new SqlConnection(cstr))
                {
                    SqlCommand cmd = new SqlCommand("SELECT * FROM whatever WHERE id = 5", conn);
                    try
                    {
                        conn.Open();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private string GetConnectionString(Advert advert)
        {
            if (advert.City == "Москва" && advert.Usedtype == Usedtype.New)
                return "Server=88.212.209.125;Database=moskva;Uid=moskva;Pwd=gfdkjdfh;Charset=utf8;Default Command Timeout=300000;";
            else
                return null;
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

        [Required]
        public virtual Advert Advert { get; set; }
        public DateTime DateOfExport { get; set; }
    }
}
