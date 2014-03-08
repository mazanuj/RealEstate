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
        private static object _lock = new object();

        [ImportingConstructor]
        public AdvertsManager(RealEstateContext context)
        {
            this._context = context;
        }

        public void Save(Advert advert, ParserSetting setting)
        {
            lock (_lock)
            {
                var oldAdvert = _context.Adverts.SingleOrDefault(a => a.Url == advert.Url);
                if (oldAdvert == null)
                {

                    if (advert.ExportSites != null)
                    {
                        if (!advert.ExportSites.Any(e => e.Id == setting.ExportSite.Id))
                            advert.ExportSites.Add(setting.ExportSite);
                    }
                    else
                        advert.ExportSites = new List<Exporting.ExportSite>() { setting.ExportSite };

                    _context.Adverts.Add(advert);
                }
                else
                {
                    if (oldAdvert.ExportSites == null)
                        oldAdvert.ExportSites = new List<Exporting.ExportSite>();
                    if (!oldAdvert.ExportSites.Contains(setting.ExportSite))
                        oldAdvert.ExportSites.Add(setting.ExportSite);


                    oldAdvert.MessageFull = advert.MessageFull;
                    oldAdvert.Price = advert.Price;
                    if (!String.IsNullOrEmpty(advert.Distinct))
                        oldAdvert.PhoneNumber = advert.PhoneNumber;

                    if (oldAdvert.DateSite < advert.DateSite)
                        oldAdvert.DateSite = advert.DateSite;

                    oldAdvert.DateUpdate = DateTime.Now;
                    if (!String.IsNullOrEmpty(advert.Distinct))
                        oldAdvert.Name = advert.Name;

                    if (!String.IsNullOrEmpty(advert.Distinct))
                        oldAdvert.Title = advert.Title;

                    oldAdvert.ParsingNumber = advert.ParsingNumber;

                    if (!String.IsNullOrEmpty(advert.Distinct))
                        oldAdvert.Address = advert.Address;

                    if (!String.IsNullOrEmpty(advert.Distinct))
                        oldAdvert.Distinct = advert.Distinct;

                    if (!String.IsNullOrEmpty(advert.MetroStation))
                        oldAdvert.MetroStation = advert.MetroStation;

                    if (!String.IsNullOrEmpty(advert.AO))
                        oldAdvert.AO = advert.AO;

                    if (advert.AreaFull != 0)
                        oldAdvert.AreaFull = advert.AreaFull;

                    if (advert.AreaKitchen != 0)
                        oldAdvert.AreaKitchen = advert.AreaKitchen;

                    if (advert.AreaLiving != 0)
                        oldAdvert.AreaLiving = advert.AreaLiving;

                }
                _context.SaveChanges(); 
            }
        }

        public void Save(Advert advert)
        {
            _context.SaveChanges();
        }

        public void Delete(Advert advert)
        {
            _context.Adverts.Remove(advert);
            _context.SaveChanges();
        }

        public void DeleteAll()
        {
            // _context.Database.ExecuteSqlCommand("TRUNCATE TABLE adverts;"); //it just not work WTF???
            foreach (var advert in _context.Adverts)
            {
                _context.Adverts.Remove(advert);
            }

            _context.SaveChanges();
        }

        public IEnumerable<Advert> Filter(IEnumerable<Advert> adverts, UniqueEnum filter)
        {
            switch (filter)
            {
                case UniqueEnum.All:
                    return adverts;
                case UniqueEnum.New:
                    return FilterNew(adverts);
                case UniqueEnum.Unique:
                    return FilterUnique(adverts);
                default:
                    throw new NotImplementedException();
            }
        }

        private IEnumerable<Advert> FilterNew(IEnumerable<Advert> adverts)
        {
            var result = new List<Advert>();

            foreach (var item in adverts)
            {
                if (!adverts.Any(a => a.PhoneNumber == item.PhoneNumber && a.MessageFull == item.MessageFull && a.Id != item.Id))
                    result.Add(item);
            }

            return result;
        }

        private IEnumerable<Advert> FilterUnique(IEnumerable<Advert> adverts)
        {
            var result = new List<Advert>();


            foreach (var item in adverts)
            {
                if (!adverts.Any(a => a.PhoneNumber == item.PhoneNumber && a.Id != item.Id))
                    result.Add(item);
            }

            return result;
        }


        public bool IsAdvertNew(Advert item)
        {
            return !_context.Adverts.Any(a => a.PhoneNumber == item.PhoneNumber && a.MessageFull == item.MessageFull && a.Id != item.Id);
        }

        public bool IsAdvertUnique(Advert item)
        {
            return !_context.Adverts.Any(a => a.PhoneNumber == item.PhoneNumber && a.Id != item.Id);
        }

        private int _lastParsingNumber = -1;
        public int LastParsingNumber
        {
            get
            {
                if (_lastParsingNumber == -1)
                {
                    using (var context = new RealEstateContext())
                    {
                        if (context.Adverts.FirstOrDefault() == null)
                            _lastParsingNumber = 0;
                        else
                            _lastParsingNumber = context.Adverts.Max(a => a.ParsingNumber);
                    }
                }
                return _lastParsingNumber;
            }
        }

        public void IncrementParsingNumber()
        {
            var dumb = LastParsingNumber;
            _lastParsingNumber++;
        }
        internal bool IsParsed(string url)
        {
            using (var context = new RealEstateContext())
            {
                return context.Adverts.Any(u => u.Url == url);
            }
        }

        internal Advert GetParsed(string url)
        {
            return _context.Adverts.FirstOrDefault(u => u.Url == url);
        }
    }

    public enum UniqueEnum
    {
        All,
        New,
        Unique
    }
}
