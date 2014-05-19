using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RealEstate.Exporting.Exporters
{
    public class MockExporter : ExporterBase
    {
        public override string SavePhotos(Parsing.Advert advert, ExportSite site, object id)
        {
            Thread.Sleep(1000);
            return "";
        }

        public override void ExportAdvert(Parsing.Advert advert, ExportSite site, ExportSetting setting)
        {
            Thread.Sleep(1000);
        }
    }
}
