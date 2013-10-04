using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportingManager))]
    public class ExportingManager
    {
        private readonly RealEstateContext context = null;

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context)
        {
            this.context = context;
        }


        public IEnumerable<Advert> Filter(IEnumerable<Advert> adverts, ExportStatus status)
        {
            return adverts;
        }

    }

    public enum ExportStatus
    {
        Unprocessed,
        Exporting,
        Exported
    }
}
