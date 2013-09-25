using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Db;

namespace RealEstate.Parsing
{
    [Export(typeof(ParserSettingManager))]
    public class ParserSettingManager
    {
        private RealEstateContext context;

        [ImportingConstructor]
        public ParserSettingManager(RealEstateContext context)
        {
            this.context = context;
        }

        public ParserSetting Exists(ParserSetting setting)
        {
            if (setting == null) return null;

            var set = from s in context.ParserSettings
                      where
                        s.AdvertTypeValue == (int)setting.AdvertType &&
                        s.City == setting.City &&
                        s.ExportSite.Id == setting.ExportSite.Id &&
                        s.ImportSiteValue == (int)setting.ImportSite &&
                        s.ParsePeriodValue == (int)setting.ParsePeriod &&
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

        public List<ParserSetting> FindSettings(AdvertType advertType, string city, ImportSite site, ParsePeriod period, RealEstateType type, Usedtype subtype)
        {
            var set = from s in context.ParserSettings
                      where
                        s.AdvertTypeValue == (int)advertType &&
                        s.City == city &&
                        s.ImportSiteValue == (int)site &&
                        s.ParsePeriodValue == (int)period &&
                        s.RealEstateTypeValue == (int)type &&
                        s.UsedtypeValue == (int)subtype
                      select s;

            return set.ToList();
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

        public void SaveUrls(IEnumerable<ParserSourceUrl> urls, ParserSetting setting)
        {
            foreach (var url in urls)
            {
                var oldUrl = (from u in context.ParserSourceUrls
                              where u.Id == url.Id
                              select u).FirstOrDefault();

                if (oldUrl != null)
                {
                    oldUrl.Url = url.Url;
                }
                else
                {
                    context.ParserSourceUrls.Add(url);
                }
            }

            var forDeleting = setting.Urls.Where(url => !urls.Contains(url)).ToList();

            foreach (var url in forDeleting)
            {
                context.ParserSourceUrls.Remove(url);
            }

            context.SaveChanges();

        }

        public List<UsedTypeNamed> SubTypes(RealEstateType type)
        {
            List<UsedTypeNamed> subs = new List<UsedTypeNamed>();
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
            List<AdvertTypeNamed> subs = new List<AdvertTypeNamed>();
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.All, Name = "Все" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Buy, Name = "Куплю" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Pass, Name = "Сниму" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Rent, Name = "Сдам" });
            subs.Add(new AdvertTypeNamed() { Type = AdvertType.Sell, Name = "Продам" });

            return subs;
        }

        public List<RealEstatetypeNamed> RealEstateTypes()
        {
            List<RealEstatetypeNamed> subs = new List<RealEstatetypeNamed>();
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
