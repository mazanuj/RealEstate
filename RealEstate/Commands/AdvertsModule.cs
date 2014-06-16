using Caliburn.Micro;
using RealEstate.Parsing;
using RealEstate.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Action = System.Action;

namespace RealEstate.Commands
{
    public class AdvertsModule : Module
    {
        private readonly AdvertsManager _manager;
        private readonly IWindowManager _windowManager;

        public AdvertsModule(AdvertsManager manager, IWindowManager windowManager)
        {
            _manager = manager;
            _windowManager = windowManager;
        }

        public override bool Process(string[] args)
        {
            if (base.Process(args))
                return true;

            var count = args.Count();

            if (count == 3 && args[1] == "show")
            {
                int id;
                if (Int32.TryParse(args[2], out id))
                    ShowAdvert(id);
                else
                    Write("Id param is invalid");
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

        public bool ShowAdvert(int advId)
        {
            Task.Factory.StartNew(() =>
            {
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    try
                    {
                        var style = new Dictionary<string, object>();
                        style.Add("style", "VS2012ModalWindowStyle");

                        var model = IoC.Get<AdvertViewModel>();
                        model.AdvertId = advId;
                        _windowManager.ShowDialog(model, settings: style);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString(), "Error!");
                    }
                }));
            });

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
