﻿using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using RealEstate.Db;

namespace RealEstate.Parsing
{
    [Export(typeof(ParserSettingManager))]
    public class ParserSettingManager
    {
        private readonly RealEstateContext context;
        private static readonly object _lock = new object();

        [ImportingConstructor]
        public ParserSettingManager(RealEstateContext context)
        {
            this.context = context;
        }

        public ParserSetting Exists(ParserSetting setting)
        {
            lock (_lock)
            {
                if (setting == null) return null;

                var set = from s in context.ParserSettings
                          where
                            s.AdvertTypeValue == (int)setting.AdvertType &&
                            s.ExportSite.Id == setting.ExportSite.Id &&
                            s.ImportSiteValue == (int)setting.ImportSite &&
                            s.RealEstateTypeValue == (int)setting.RealEstateType &&
                            s.UsedtypeValue == (int)setting.Usedtype
                          select s;

                if (set != null)
                    if (set.Count() > 0)
                    {
                        return set.First();
                    }

                return setting; 
            }

        }

        public List<ParserSetting> FindSettings(AdvertType advertType, IEnumerable<string> cities, ImportSite site, RealEstateType type, Usedtype subtype)
        {
            lock (_lock)
            {
                var set = from s in context.ParserSettings.Include("Urls")
                          where
                            (s.AdvertTypeValue == (int)advertType || advertType == AdvertType.All) &&
                            (s.ImportSiteValue == (int)site || site == ImportSite.All) &&
                            s.RealEstateTypeValue == (int)type &&
                            (s.UsedtypeValue == (int)subtype || subtype == Usedtype.All) &&
                            (cities.Contains(s.ExportSite.City) || cities.Contains("Все"))
                          select s;

                return set.ToList(); 
            }
        }

        public void SaveParserSetting(ParserSetting setting)
        {
            if (setting.Id == 0)
            {
                if (setting.ExportSite.ParseSettings == null)
                    setting.ExportSite.ParseSettings = new List<ParserSetting>();

                setting.ExportSite.ParseSettings.Add(setting);
                context.SaveChanges();
            }
        }

        public void DeleteAll()
        {
            foreach (var url in context.SourceUrls.ToList())
            {
                context.SourceUrls.Remove(url);
            }
        }

        public void SaveUrls(IEnumerable<ParserSourceUrl> urls, ParserSetting setting)
        {
            foreach (var url in urls)
            {
                var oldUrl = (from u in context.SourceUrls
                              where u.Id == url.Id
                              select u).FirstOrDefault();

                if (oldUrl != null)
                {
                    oldUrl.Url = url.Url;
                }
                else
                {
                    context.SourceUrls.Add(url);
                }
            }

            if (setting.Urls != null)
            {
                var forDeleting = setting.Urls.Where(url => !urls.Contains(url)).ToList();

                foreach (var url in forDeleting)
                {
                    context.SourceUrls.Remove(url);
                }
            }

            context.SaveChanges();

        }

        public List<UsedTypeNamed> SubTypes(RealEstateType type)
        {
            var subs = new List<UsedTypeNamed>();
            subs.Add(new UsedTypeNamed() { Type = Usedtype.All, Name = "Все" });
            switch (type)
            {
                case RealEstateType.Apartments:
                    subs.Add(new UsedTypeNamed() { Type = Usedtype.New, Name = "Новые" });
                    subs.Add(new UsedTypeNamed() { Type = Usedtype.Used, Name = "Вторичное" });
                    break;
                case RealEstateType.House:
                    break;
            }

            return subs;
        }

        public List<AdvertTypeNamed> AdvertTypes()
        {
            var subs = new List<AdvertTypeNamed>();
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.All, Name = "Все" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Buy, Name = "Куплю" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Pass, Name = "Сниму" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Rent, Name = "Сдам" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Sell, Name = "Продам" });

            return subs;
        }

        public List<RealEstatetypeNamed> RealEstateTypes()
        {
            var subs = new List<RealEstatetypeNamed>();
            subs.Add(new RealEstatetypeNamed() { Type = RealEstateType.All, Name = "Все" });
            subs.Add(new RealEstatetypeNamed() { Type = RealEstateType.Apartments, Name = "Квартиры" });
            subs.Add(new RealEstatetypeNamed() { Type = RealEstateType.House, Name = "Дома" });
            return subs;
        }

    }

    public class UsedTypeNamed
    {
        public Usedtype Type { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }

    public class AdvertTypeNamed
    {
        public AdvertType Type { get; set; }
        public string Name { get; set; }
    }

    public class RealEstatetypeNamed
    {
        public RealEstateType Type { get; set; }
        public string Name { get; set; }
    }

}
