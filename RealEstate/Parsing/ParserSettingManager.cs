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
        private RealEstateContext context = new RealEstateContext();

        public bool Exists(ParserSetting setting)
        {
            if (setting == null) return false;

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
                    return true;

            return false;

        }


    }
}
