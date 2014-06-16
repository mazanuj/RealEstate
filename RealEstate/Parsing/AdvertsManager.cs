using RealEstate.Db;
using RealEstate.Exporting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

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

        public void Save(Advert advert, int[] exportSiteIds, bool onlyPhone)
        {
            if(advert == null)
            {
                Trace.WriteLine("Saving null advert", "Code error");
                return;
            }

            lock (_lock)
            {
                using (var context = new RealEstateContext())
                {
                    var oldAdvert = context.Adverts.FirstOrDefault(a => a.Url == advert.Url);
                    var exportSites = context.ExportSites.Where(e => exportSiteIds.Contains(e.Id));


                    if (oldAdvert == null || onlyPhone)
                    {
                        //Trace.WriteLine("Creating");

                        context.Adverts.Add(advert);
                        context.SaveChanges();

                        if (advert.ExportSites == null)
                            advert.ExportSites = new List<ExportSite>();

                        foreach (var item in exportSites)
                        {
                            if (!advert.ExportSites.Any(e => e.Id == item.Id))
                                advert.ExportSites.Add(item);
                        }
                    }
                    else
                    {
                        //Trace.WriteLine("Editing");

                        oldAdvert.ParsingNumber = advert.ParsingNumber;

                        if (oldAdvert.ExportSites == null)
                            oldAdvert.ExportSites = new List<ExportSite>();

                        foreach (var item in exportSites)
                        {
                            if (!oldAdvert.ExportSites.Any(e => e.Id == item.Id))
                                oldAdvert.ExportSites.Add(item);
                        }

                        oldAdvert.Address = advert.Address;
                        oldAdvert.Street = advert.Street;
                        oldAdvert.House = advert.House;
                        oldAdvert.HousePart = advert.HousePart;
                        oldAdvert.HouseStroenie = advert.HouseStroenie;
                        oldAdvert.AreaFull = advert.AreaFull;
                        oldAdvert.AreaKitchen = advert.AreaKitchen;
                        oldAdvert.AreaLiving = advert.AreaLiving;
                        oldAdvert.City = advert.City;
                        oldAdvert.DateUpdate = advert.DateUpdate;
                        oldAdvert.PhoneNumber = advert.PhoneNumber;
                    }

                    context.SaveChanges();  
                }
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
            Trace.WriteLine("Total:" + _context.Adverts.Count());
            // _context.Database.ExecuteSqlCommand("TRUNCATE TABLE adverts;"); //it just not work WTF???
            foreach (var advert in _context.Adverts.OrderByDescending(a => a.DateSite))
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
