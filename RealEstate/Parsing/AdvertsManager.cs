using RealEstate.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.Parsing
{
    [Export(typeof(AdvertsManager))]
    public class AdvertsManager
    {
        private readonly RealEstateContext _context = null;

        [ImportingConstructor]
        public AdvertsManager(RealEstateContext context)
        {
            this._context = context;
        }

        public void Save(Advert advert, ParserSetting setting)
        {
            var url = advert.Url;
            var oldAdvert = _context.Adverts.SingleOrDefault(a => a.Url == url);
            if (oldAdvert == null)
            {
                advert.ExportSites = new List<Exporting.ExportSite>() { setting.ExportSite };
                _context.Adverts.Add(advert);
            }
            else
            {
                if (oldAdvert.ExportSites == null) advert.ExportSites = new List<Exporting.ExportSite>();
                if(!oldAdvert.ExportSites.Contains(setting.ExportSite))
                    oldAdvert.ExportSites.Add(setting.ExportSite);

                if (oldAdvert.DateSite != advert.DateSite)
                {
                    oldAdvert.MessageFull = advert.MessageFull;
                    oldAdvert.Price = advert.Price;
                    oldAdvert.PhoneNumber = advert.PhoneNumber;
                    oldAdvert.DateSite = advert.DateSite;
                    oldAdvert.DateUpdate = DateTime.Now;
                    oldAdvert.Name = advert.Name;
                    oldAdvert.Title = advert.Title;
                }
            }

            _context.SaveChanges();
        }
    }
}
