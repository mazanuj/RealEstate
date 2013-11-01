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
                    return adverts.Where(a => IsAdvertNew(a, adverts)).AsParallel();
                case UniqueEnum.Unique:
                    return adverts.Where(a => IsAdvertUnique(a, adverts)).AsParallel();
                default:
                    throw new NotImplementedException();
            }
        }

        private bool IsAdvertNew(Advert advert, IEnumerable<Advert> adverts)
        {
            var items = from a in adverts
                        where a.Id != advert.Id
                        && ((a.PhoneNumber == advert.PhoneNumber
                        && a.MessageFull != advert.MessageFull)
                        || a.MessageFull == advert.MessageFull)
                        select a;

            return !items.Any(a => a.DateSite > advert.DateSite); //todo search from exported
        }

        private bool IsAdvertUnique(Advert advert, IEnumerable<Advert> adverts)
        {
            var items = from a in adverts
                        where a.Id != advert.Id
                        && (a.PhoneNumber == advert.PhoneNumber
                        || a.MessageFull == advert.MessageFull)
                        select a;

            return items.Count() == 0;
        }

        private int _lastParsingNumber = -1;
        public int LastParsingNumber
        {
            get
            {
                if (_lastParsingNumber == -1)
                {
                    if (_context.Adverts.FirstOrDefault() == null)
                        _lastParsingNumber = 0;
                    else
                        _lastParsingNumber = _context.Adverts.Max(a => a.ParsingNumber);
                }
                return _lastParsingNumber;
            }
        }

        public void IncrementParsingNumber()
        {
            var dumb = LastParsingNumber;
            _lastParsingNumber++;
        }
    }

    public enum UniqueEnum
    {
        All,
        New,
        Unique
    }
}
