using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.Commands
{
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
            get { return "Adverts removing"; }
        }

        public override string Help
        {
            get { return "advert \r\n\t\t [remove [-all]]"; }
        }
    }

}
