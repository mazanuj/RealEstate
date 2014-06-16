using System.Threading;
using RealEstate.Parsing;

namespace RealEstate.Exporting.Exporters
{
    public class MockExporter : ExporterBase
    {
        public override string SavePhotos(Advert advert, ExportSite site, object id)
        {
            Thread.Sleep(1000);
            return "";
        }

        public override void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting)
        {
            Thread.Sleep(1000);
        }
    }
}
