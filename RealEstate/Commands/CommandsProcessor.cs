using RealEstate.Db;
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
        private readonly RealEstateContext _context = null;
        private readonly Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        [ImportingConstructor]
        public CommandsProcessor(RealEstateContext context, AdvertsManager advertsManager)
        {
            _context = context;
            Modules.Add("advert", new AdvertsModule(advertsManager));
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
            if (args[0] == "cancel" || args[0] == "n" || args[0] == "no" || args[0] == "exit")
            {
                WaitForResponse = false;
                Write("Command canceled");
                return true;
            }

            if (args.Count() > 1 && (args[1] == "man" || args[1] == "help"))
            {
                Write(this.Help);
                return false;
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

            if (count == 1 && (args[0] == "y" || args[0] == "yes"))
            {
                if (SuredAction != null)
                    return SuredAction.Invoke();
            }
            else if (count >= 3 && args[1].Contains("rem") && args[2].Contains("all"))
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
}
