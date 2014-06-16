using RealEstate.Parsing;
using System;
using System.Diagnostics;
using System.Linq;

namespace RealEstate.Commands
{
    public class ImportModule : Module
    {
        private readonly ParserSettingManager _manager;
        public ImportModule(ParserSettingManager manager)
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

            return true;

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
            get { return "Import url removing"; }
        }

        public override string Help
        {
            get { return "urls \r\n\t\t [remove [-all]]"; }
        }
    }
}
