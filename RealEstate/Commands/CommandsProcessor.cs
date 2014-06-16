using Caliburn.Micro;
using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using LogManager = RealEstate.Log.LogManager;

namespace RealEstate.Commands
{
    [Export(typeof(CommandsProcessor))]
    public class CommandsProcessor
    {
        private readonly Dictionary<string, Module> Modules = new Dictionary<string, Module>();

        [ImportingConstructor]
        public CommandsProcessor(AdvertsManager advertsManager, LogManager logManager, 
            RealEstateContext context, IWindowManager windowManager,
            ParserSettingManager parserSourceManager)
        {
            Modules.Add("advert", new AdvertsModule(advertsManager, windowManager));
            Modules.Add("log", new LogModule(logManager));
            Modules.Add("urls", new ImportModule(parserSourceManager));
        }

        private Module LastModule;

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
                Write(Help);
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
}
