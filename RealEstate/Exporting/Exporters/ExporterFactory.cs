using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Exporting.Exporters
{
    public class ExporterFactory
    {
        protected readonly ImagesManager _imagesManager;
        protected readonly PhonesManager _phonesManager;

        public ExporterFactory(ImagesManager images, PhonesManager phones)
        {
            _imagesManager = images;
            _phonesManager = phones;
        }


        public ExporterBase GetExporter(string databaseName)
        {
            //return new MockExporter();

            switch (databaseName)
            {
                case "kupi":
                    return new KupiYaroslavlExporter(_imagesManager, _phonesManager);
                case "new_nov":
                    return new NovoYaroslavlExporter(_imagesManager, _phonesManager);
                default:
                    return new MoskvaExporter(_imagesManager, _phonesManager);
            }
        }
    }
}
