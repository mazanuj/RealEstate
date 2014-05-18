using Caliburn.Micro;
using MySql.Data.MySqlClient;
using RealEstate.Db;
using RealEstate.Exporting.Exporters;
using RealEstate.Parsing;
using RealEstate.SmartProcessing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportingManager))]
    public class ExportingManager : PropertyChangedBase
    {
        private readonly RealEstateContext _context = null;
        private readonly ImagesManager _imagesManager = null;
        private readonly SmartProcessor _processr = null;
        private readonly PhonesManager _phonesManager = null;
        private readonly ExporterFactory _factory;

        public ObservableCollection<ExportItem> ExportQueue = null;

        private bool _stopped = false;

        private bool _IsWaiting = false;
        public bool IsWaiting
        {
            get { return _IsWaiting; }
            set
            {
                _IsWaiting = value;
                NotifyOfPropertyChange(() => IsWaiting);
                NotifyOfPropertyChange(() => StringStatus);
            }
        }

        public string StringStatus
        {
            get { return IsWaiting ? "Отправка объявлений...." : "Ожидание начала экспорта..."; }
        }


        private static bool IsStarted = false;
        private static object _lock = new object();

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context, ImagesManager images, SmartProcessor processor, PhonesManager phonesManager)
        {
            _context = context;
            _imagesManager = images;
            _processr = processor;
            _phonesManager = phonesManager;
            ExportQueue = new ObservableCollection<ExportItem>();
            ExportQueue.CollectionChanged += ExportQueue_CollectionChanged;

            _factory = new Exporters.ExporterFactory(_imagesManager, _phonesManager);
        }

        void ExportQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && IsStarted)
            {
                lock (_lock)
                {
                    StartExportLoop(false);
                }
            }
        }

        public void StartExportLoop(bool manual)
        {
            if (IsWaiting) return;
            if (manual)
                _stopped = false;

            Task.Factory.StartNew(() =>
               {
                   IsWaiting = true;
                   int lastFailedExportedId = -1;
                   int currentId = -1;
                   int failedCount = 0;
                   while (!_stopped && ExportQueue.Any(i => !i.IsExported))
                   {
                       try
                       {
                           var item = ExportQueue.FirstOrDefault();
                           if (item != null)
                           {
                               currentId = item.Id;
                               Export(item);
                           }

                           int count = 0;
                           while (!_stopped && count < 10)
                           {
                               count++;
                               Thread.Sleep(Settings.SettingsStore.ExportInterval * 100);
                           }
                       }
                       catch (Exception ex)
                       {
                           if (lastFailedExportedId == currentId)
                               failedCount++;
                           if (failedCount > 5)
                           {
                               Trace.TraceError("Failed to export item more than 5 times. Export stoppped.", "Export error");
                               App.NotifyIcon.ShowBalloonTip("Экспорт остановлен", "Число неудачных попыток экспортирования превысило 5 раз", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
                               break;
                           }
                           lastFailedExportedId = currentId;
                           Trace.WriteLine(ex.ToString(), "Error when exporting");
                           Thread.Sleep(1000);
                       }
                   }

                   IsWaiting = false;
               }, TaskCreationOptions.LongRunning);
        }


        public IEnumerable<Advert> Filter(IEnumerable<Advert> adverts, ExportStatus status)
        {
            switch (status)
            {
                case ExportStatus.Unprocessed:
                    return adverts.Where(a => !_context.ExportItems.Any(e => e.Advert.Id == a.Id));
                case ExportStatus.Exporting:
                    return adverts.Where(a => _context.ExportItems.Any(e => !e.IsExported && (e.Advert.Id == a.Id)));
                case ExportStatus.Exported:
                    return adverts.Where(a => _context.ExportItems.Any(e => e.IsExported && (e.Advert.Id == a.Id)));
                default:
                    return null;
            }
        }

        public void RestoreQueue()
        {
            foreach (var item in _context.ExportItems.Include("Advert").Where(i => !i.IsExported).ToList())
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Add(item);
                }));
            }

            IsStarted = true;
            //StartExportLoop();
        }

        public void AddAdvertToExport(int advertId)
        {
            var advert = _context.Adverts.Find(advertId);
            if (advert == null) { Trace.WriteLine("advert is null (AddAdvertToExport)", "Code error"); return; }

            ExportQueue.Remove(null);

            if (ExportQueue.Any(e => e.Advert.Id == advert.Id && !e.IsExported))
            {
                Trace.WriteLine("Advert id = " + advert.Id + " already in the export queue");
                return;
            }

            var item = new ExportItem() { Advert = advert, DateOfExport = new DateTime(1991, 1, 1) };
            _context.ExportItems.Add(item);

            int failed = 0;
            while (failed < 5)
            {
                try
                {
                    failed++;
                    _context.SaveChanges();
                    break;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "AddAdvertToExport");
                    Thread.Sleep(200);
                }
            }

            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ExportQueue.Add(item);
            }));
        }

        public void Remove(ExportItem item)
        {
            App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Remove(item);
                }));
            _context.ExportItems.Remove(item);

            _context.SaveChanges();
        }

        public void Export(ExportItem item)
        {
            if (item == null || item.Advert == null)
            {
                Trace.WriteLine("Exporting: item == null || item.Advert == null");
                item.IsExported = true;
                return;
            }

            bool isExported = false;

            if (item.Advert.ExportSites != null && !item.IsExported)
            {
                //Trace.WriteLine("Exporting: item.Advert.ExportSites.Count = " + item.Advert.ExportSites.Count);

                foreach (var site in item.Advert.ExportSites)
                {
                    var settings = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == site.Id);
                    if (settings != null)
                    {
                        if ((settings.UsedtypeValue != item.Advert.UsedtypeValue && settings.Usedtype != Usedtype.All) ||
                            (settings.RealEstateTypeValue != item.Advert.RealEstateTypeValue && settings.RealEstateType != RealEstateType.All) ||
                            (settings.AdvertTypeValue != item.Advert.AdvertTypeValue && settings.AdvertType != AdvertType.All))
                        {
                            Trace.WriteLine("Exporting: skipped by settings");
                            continue;
                        }
                    }

                    if (!_context.ExportItems.Any(e => e.Advert.Id == item.Advert.Id && e.IsExported && e.Id != item.Id) || Settings.SettingsStore.ExportParsed)
                    {
                        var exporter = _factory.GetExporter(site.Database);
                        exporter.ExportAdvert(item.Advert, site, settings);

                        Trace.WriteLine("Advert id = " + item.Advert.Id + " is exported succesfully");

                        isExported = true;
                    }
                    else
                        Trace.WriteLine("Advert id = " + item.Advert.Id + " is skipped as already exported", "Export skipped");

                    if (settings != null)
                    {
                        int count = 0;
                        while (!_stopped && count < 60)
                        {
                            count++;
                            Thread.Sleep(settings.Delay * 1000);
                        }
                    }
                }
            }
            else
            {
                Trace.WriteLine("Exporting: item.Advert.ExportSites != null && !item.IsExported");
            }

            if (isExported)
            {
                //Trace.WriteLine("Exporting: isExported");
                item.DateOfExport = DateTime.Now;
                item.IsExported = true;
                _context.SaveChanges();
            }
            else
            {
                //Trace.WriteLine("Exporting: !isExported");
                _context.ExportItems.Remove(item);
                _context.SaveChanges();
            }

            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ExportQueue.Remove(item);
            }));
        }



        public void Stop()
        {
            _stopped = true;
        }
    }

    public enum ExportStatus
    {
        Unprocessed,
        Exporting,
        Exported
    }

    public class ExportItem
    {
        public int Id { get; set; }
        public bool IsExported { get; set; }

        [Required]
        public virtual Advert Advert { get; set; }
        public DateTime DateOfExport { get; set; }
    }
}
