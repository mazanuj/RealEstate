﻿using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Db;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        public ObservableCollection<ExportItem> Items
        {
            get { return ExportingManager.ExportQueue; }
        }

        public void OpenItem(ExportItem item)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var style = new Dictionary<string, object>();
                    style.Add("style", "VS2012ModalWindowStyle");

                    var model = IoC.Get<AdvertViewModel>();
                    model.AdvertOriginal = item.Advert;
                    _windowManager.ShowDialog(model, settings: style);
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
