using RealEstate.Db;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Commands
{
    public class TestDataModule : Module
    {
        private readonly RealEstateContext _context = null;

        public TestDataModule(RealEstateContext context)
        {
            _context = context;
        }

        public override bool Process(string[] args)
        {
            if (base.Process(args))
                return true;
            var count = args.Count();

            if (count == 2 && args[1] == "importsite")
            {
                var rows = from o in _context.ExportSites
                           select o;
                foreach (var row in rows)
                {
                    _context.ExportSites.Remove(row);
                }
                _context.SaveChanges();

                ExportSite irr = new ExportSite()
                {
                    City = "Ярославль",
                    DisplayName = "irr.ru",
                    Database = "http://yaroslavl.irr.ru/"
                };

                ExportSite avito = new ExportSite()
                {
                    City = "Ярославль",
                    DisplayName = "avito",
                    Database = "http://www.avito.ru/"
                };

                _context.ExportSites.Add(irr);
                _context.ExportSites.Add(avito);

                _context.SaveChanges();

                var pars = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = irr,
                    ImportSite = ImportSite.Hands,
                    ParsePeriod = ParsePeriod.Today,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars.Urls.Add(new ParserSourceUrl() { ParserSetting = pars, Url = "http://yaroslavl.irr.ru/real-estate/apartments-sale/" });

                irr.ParseSettings.Add(pars);

                var pars2 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = irr,
                    ImportSite = ImportSite.Hands,
                    ParsePeriod = ParsePeriod.Yesterday,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars2.Urls.Add(new ParserSourceUrl() { ParserSetting = pars2, Url = "http://yaroslavl.irr.ru/real-estate/apartments-sale/" });

                irr.ParseSettings.Add(pars2);

                var pars3 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = irr,
                    ImportSite = ImportSite.Hands,
                    ParsePeriod = ParsePeriod.Week,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars3.Urls.Add(new ParserSourceUrl() { ParserSetting = pars3, Url = "http://yaroslavl.irr.ru/real-estate/apartments-sale/" });

                irr.ParseSettings.Add(pars3);

                var pars4 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = irr,
                    ImportSite = ImportSite.Hands,
                    ParsePeriod = ParsePeriod.All,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars4.Urls.Add(new ParserSourceUrl() { ParserSetting = pars4, Url = "http://yaroslavl.irr.ru/real-estate/apartments-sale/" });

                irr.ParseSettings.Add(pars4);

                var pars5 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = avito,
                    ImportSite = ImportSite.Avito,
                    ParsePeriod = ParsePeriod.Today,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars5.Urls.Add(new ParserSourceUrl() { ParserSetting = pars5, Url = "http://www.avito.ru/yaroslavl/kvartiry/prodam" });

                avito.ParseSettings.Add(pars5);

                var pars6 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = avito,
                    ImportSite = ImportSite.Avito,
                    ParsePeriod = ParsePeriod.Yesterday,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars6.Urls.Add(new ParserSourceUrl() { ParserSetting = pars6, Url = "http://www.avito.ru/yaroslavl/kvartiry/prodam" });

                avito.ParseSettings.Add(pars6);

                var pars7 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = avito,
                    ImportSite = ImportSite.Avito,
                    ParsePeriod = ParsePeriod.Week,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars7.Urls.Add(new ParserSourceUrl() { ParserSetting = pars7, Url = "http://www.avito.ru/yaroslavl/kvartiry/prodam" });

                avito.ParseSettings.Add(pars7);

                var pars8 = new ParserSetting()
                {
                    AdvertType = AdvertType.Sell,
                    ExportSite = avito,
                    ImportSite = ImportSite.Avito,
                    ParsePeriod = ParsePeriod.All,
                    RealEstateType = RealEstateType.Apartments,
                    Usedtype = Usedtype.All
                };
                pars8.Urls.Add(new ParserSourceUrl() { ParserSetting = pars8, Url = "http://www.avito.ru/yaroslavl/kvartiry/prodam" });

                avito.ParseSettings.Add(pars8);

                _context.SaveChanges();

                Write("Ok");

            }
            else
            {
                Write("Proper command not found!");
            }

            return true;
        }
        public override string Description
        {
            get { return "Insert test data into Db"; }
        }

        public override string Help
        {
            get { return "testdata \r\n\t\t[importsite]"; }
        }
    }

}
