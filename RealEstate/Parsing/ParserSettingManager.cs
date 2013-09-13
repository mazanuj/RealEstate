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
            //todo add removing
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
    }
}
