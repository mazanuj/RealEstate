using Caliburn.Micro;
using RealEstate.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Windows;
using Whush.Demo.Styles.VS2012;

namespace RealEstate
{
    public class AppBootstrapper : Bootstrapper<MainViewModel>
    {
        private CompositionContainer container;

        protected override void Configure()
        {
            container = new CompositionContainer(new AggregateCatalog(AssemblySource.Instance.Select(x => new AssemblyCatalog(x)).OfType<ComposablePartCatalog>()));

            CompositionBatch batch = new CompositionBatch();

            batch.AddExportedValue<IWindowManager>(new CustomWindowManager());
            batch.AddExportedValue<IEventAggregator>(new EventAggregator());
            batch.AddExportedValue(container);

            container.Compose(batch);
        }

        protected override object GetInstance(Type serviceType, string key)
        {
            string contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
            var exports = container.GetExportedValues<object>(contract);

            if (exports.Count() > 0)
            {
                return exports.First();
            }

            throw new Exception(string.Format("Could not locate any instances of contract {0}.", contract));
        }
    }

    public class CustomWindowManager : WindowManager
    {
        protected override Window CreateWindow(object rootModel, bool isDialog, object context, IDictionary<string, object> settings)
        {
            var window = base.CreateWindow(rootModel, isDialog, context, settings);

            if (settings != null && settings.ContainsKey("style"))
                window.Style = (System.Windows.Style)App.Current.Resources[settings["style"]];
            else
                window.Style = (System.Windows.Style)App.Current.Resources["VS2012WindowStyle"];
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            return window;
        }

    }
}
