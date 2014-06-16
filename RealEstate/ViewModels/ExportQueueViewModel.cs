using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Db;
using RealEstate.Exporting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.ViewModels
{
    [Export(typeof(ExportQueueViewModel))]
    public class ExportQueueViewModel : ValidatingScreen<ExportQueueViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly RealEstateContext _context;
        private readonly IEventAggregator _events;
        private readonly IWindowManager _windowManager;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly Settings.SettingsManager _settingManager;
        public ExportingManager ExportingManager { get; set; }

        [ImportingConstructor]
        public ExportQueueViewModel(IEventAggregator events, RealEstateContext context,
            ExportSiteManager exportSiteManager, ExportingManager exportingManager, IWindowManager windowManager,
            Settings.SettingsManager settings)
        {
            ExportingManager = exportingManager;
            _exportSiteManager = exportSiteManager;
            _context = context;
            _events = events;
            _windowManager = windowManager;
            _settingManager = settings;
            events.Subscribe(this);
            DisplayName = "Очередь экспорта";
            _Items = ExportingManager.ExportQueue;
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }


        protected override void OnActivate()
        {
            base.OnActivate();
            if (!RealEstate.Db.RealEstateContext.isOk) return;
            ExportDelay = Settings.SettingsStore.ExportInterval;
        }

        private ObservableCollection<ExportItem> _Items;
        public ObservableCollection<ExportItem> Items
        {
            get { return _Items; }
            set
            {
                _Items = value;
                NotifyOfPropertyChange(() => Items);
            }
        }

        public void OpenItem(ExportItem item)
        {
            Task.Factory.StartNew(() =>
            {
                Trace.WriteLine("Open item:" + item.Advert.Id);

                try
                {
                    var style = new Dictionary<string, object>();
                    style.Add("style", "VS2012ModalWindowStyle");

                    var model = IoC.Get<AdvertViewModel>();
                    model.AdvertOriginal = item.Advert;
                    _windowManager.ShowDialog(model, settings: style);

                    Items = null;
                    Items = ExportingManager.ExportQueue;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void RemoveItem(ExportItem item)
        {
            try
            {
                ExportingManager.Remove(item);
            }
            catch (Exception ex)
            {
                _events.Publish("Ошибка удаления!");
                Trace.WriteLine(ex, "Error");
            }
        }

        public void BanItem(ExportItem item)
        {
            try
            {
                ExportingManager.Ban(item);
            }
            catch (Exception ex)
            {
                _events.Publish("Ошибка бана!");
                Trace.WriteLine(ex, "Error");
            }
        }

        public void OpenUrl(ExportItem item)
        {
            if (item != null && item.Advert != null)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Process.Start(item.Advert.Url);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString());
                        _events.Publish("Ошибка");
                    }
                }, CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskScheduler.Default);
            }
        }

        public void ForceExport(ExportItem item)
        {
            try
            {
                Task.Factory.StartNew(() =>
                    {
                        ExportingManager.Export(item);
                    }, TaskCreationOptions.LongRunning);
            }
            catch (Exception ex)
            {
                _events.Publish("Ошибка экспорта!");
                Trace.WriteLine(ex, "Error");
            }
        }

        public void ClearAll()
        {

            try
            {
                while (Items.Any())
                {
                    ExportingManager.Remove(Items.First());
                }
            }
            catch (Exception ex)
            {
                _events.Publish("Ошибка удаления!");
                Trace.WriteLine(ex, "Error");
            }
        }

        public void Start()
        {
            ExportingManager.StartExportLoop(true);
        }

        public void Stop()
        {
            ExportingManager.Stop();
        }

        private int _ExportDelay;

        [Required]
        [Range(0, 100000)]
        public int ExportDelay
        {
            get { return _ExportDelay; }
            set
            {
                _ExportDelay = value;
                NotifyOfPropertyChange(() => ExportDelay);
            }
        }


        public void Save()
        {
            try
            {
                Settings.SettingsStore.ExportInterval = ExportDelay;
                _settingManager.Save();
                _events.Publish("Сохранено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                _events.Publish("Ошибка сохранения");
            }
        }

        public bool CanSave
        {
            get { return !HasErrors; }
        }
    }
}
