using RealEstate.Db;
using RealEstate.Exporting;
using RealEstate.Log;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate.Commands
{
    [Export(typeof(CommandsProcessor))]
    public class CommandsProcessor
    {
        private readonly Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        [ImportingConstructor]
        public CommandsProcessor(AdvertsManager advertsManager, LogManager logManager, RealEstateContext context)
        {
            Modules.Add("advert", new AdvertsModule(advertsManager));
            Modules.Add("log", new LogModule(logManager));
            Modules.Add("testdata", new TestDataModule(context));
        }

        private Module LastModule = null;

        public void ProcessCommand(string command)
        {

            var parts = command.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Count() > 0)
            {
                if (parts[0] == "man" || parts[0] == "help")
                {
                    ShowHelp();
                    return;
                }

                if (LastModule == null)
                {
                    if (Modules.ContainsKey(parts[0]))
                        LastModule = Modules[parts[0]];
                    else
                    {
                        Module.Write("Command not found!");
                        return;
                    }
                }

                if (LastModule.Process(parts))
                    LastModule = null;

            }

        }

        private void ShowHelp()
        {
            Module.Write(String.Join("\r\n", Modules.Select(module => module.Key + ":  " + module.Value.Description)));
        }
    }

    public abstract class Module
    {
        public virtual bool Process(string[] args)
        {
            if (args[0] == "y" || args[0] == "yes")
            {
                if (SuredAction != null)
                    return SuredAction.Invoke();
            }
            else if (args[0] == "cancel" || args[0] == "n" || args[0] == "no" || args[0] == "exit")
            {
                WaitForResponse = false;
                Write("Command canceled");
                return true;
            }
            else if (args.Count() > 1 && (args[1] == "man" || args[1] == "help"))
            {
                Write(this.Help);
                return true;
            }

            return false;
        }

        public bool WaitForResponse { get; set; }
        public Func<bool> SuredAction { get; set; }

        public void BeSure(Func<bool> action)
        {
            SuredAction = action;
            Write("Are you sure? (y/n)");
        }

        public const string Char = "> ";
        public static void Write(string message)
        {
            Trace.WriteLine(Char + message);
        }

        public abstract string Description { get; }
        public abstract string Help { get; }
    }

    public class AdvertsModule : Module
    {
        private readonly AdvertsManager _manager = null;

        public AdvertsModule(AdvertsManager manager)
        {
            _manager = manager;
        }

        public override bool Process(string[] args)
        {
            if (base.Process(args))
                return true;

            var count = args.Count();

            if (count >= 3 && args[1].Contains("rem") && args[2].Contains("all"))
            {
                BeSure(RemoveAll);
                return false;
            }
            else
            {
                Write("Proper command not found!");
            }

            return false;

        }

        private bool RemoveAll()
        {
            try
            {
                _manager.DeleteAll();
                Write("deleted");
            }
            catch (Exception ex)
            {
                Write("error");
                Trace.WriteLine(ex.ToString());
                throw;
            }

            return true;
        }

        public override string Description
        {
            get { return "Adverts removing"; }
        }

        public override string Help
        {
            get { return "advert \r\n\t\t [remove [-all]]"; }
        }
    }

    public class LogModule : Module
    {
        private readonly LogManager _logManager = null;

        public LogModule(LogManager logManager)
        {
            _logManager = logManager;
        }

        public override bool Process(string[] args)
        {
            if (base.Process(args))
                return true;
            var count = args.Count();

            if (count == 2 && args[1] == "clear")
            {
                _logManager.ClearLogFile();
            }
            else
            {
                Write("Proper command not found!");
            }

            return false;
        }
        public override string Description
        {
            get { return "Manage log file"; }
        }

        public override string Help
        {
            get { return "log \r\n\t\t[clear]"; }
        }
    }

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
                    Url = "http://yaroslavl.irr.ru/"
                };

                ExportSite avito = new ExportSite()
                {
                    City = "Ярославль",
                    DisplayName = "avito",
                    Url = "http://www.avito.ru/"
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
                pars5.Urls.Add(new ParserSourceUrl() { ParserSetting = pars5, Url = "http://www.avito.ru/yaroslavskaya_oblast/kvartiry/prodam?user=1" });

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
                pars6.Urls.Add(new ParserSourceUrl() { ParserSetting = pars6, Url = "http://www.avito.ru/yaroslavskaya_oblast/kvartiry/prodam?user=1" });

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
                pars7.Urls.Add(new ParserSourceUrl() { ParserSetting = pars7, Url = "http://www.avito.ru/yaroslavskaya_oblast/kvartiry/prodam?user=1" });

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
                pars8.Urls.Add(new ParserSourceUrl() { ParserSetting = pars8, Url = "http://www.avito.ru/yaroslavskaya_oblast/kvartiry/prodam?user=1" });

                avito.ParseSettings.Add(pars8);

                _context.SaveChanges();

            }
            else
            {
                Write("Proper command not found!");
            }

            return false;
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
