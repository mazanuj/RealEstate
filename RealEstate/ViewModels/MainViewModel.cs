﻿using System.Data;
using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Diagnostics;
using RealEstate.OKATO;
using RealEstate.Settings;
using System.Threading.Tasks;
using RealEstate.Log;
using System.Timers;
using RealEstate.Db;
using System.Threading;
using RealEstate.Proxies;
using RealEstate.City;
using RealEstate.Exporting;
using RealEstate.Parsing;
using RealEstate.SmartProcessing;
using RealEstate.Views;
using LogManager = RealEstate.Log.LogManager;
using Timer = System.Timers.Timer;

namespace RealEstate.ViewModels
{
    [Export(typeof (MainViewModel))]
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<string>, IHandle<CriticalErrorEvent>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _events;
        private readonly LogManager _logManager;
        private readonly ImportManager _importManager;
        private readonly CityManager _cityManager;
        private readonly Timer _statusTimer;
        private readonly ConsoleViewModel ConsoleViewModel;
        private readonly BlackListViewModel BlackListViewModel;
        private readonly SettingsViewModel SettingsViewModel;
        private ParsingViewModel ParsingViewModel;
        private ParserSettingViewModel ParserSettingViewModel;
        private AdvertsViewModel AdvertsViewModel;

        [ImportingConstructor]
        public MainViewModel(IWindowManager windowManager, IEventAggregator events,
            ProxyManager proxyManager, CityManager cityManager, ExportSiteManager exportSiteManager,
            ConsoleViewModel consoleViewModel, LogManager logManager, SettingsManager settingsManager,
            ImportManager importManager, RulesManager rulesManager, OKATODriver okatoDriver,
            SettingsViewModel settingsViewModel, ProxiesViewModel proxiesViewModel,
            ParsingViewModel parsingViewModel, ParserSettingViewModel parserSettingViewModel,
            AdvertsViewModel advertsViewModel, ExportSettingsViewModel exportSettingsViewModel,
            ExportingManager exportingManager, TestParsingViewModel testParsingViewModel,
            StatisticsViewModel statViewModel, RulesViewModel rulesView, ExportQueueViewModel exportQueueView,
            PhonesViewModel phonesViewModel, PhonesManager phoneManager, BlackListViewModel blackListViewModel,
            CitiesViewModel citiesViewModel)
        {
            _windowManager = windowManager;
            _logManager = logManager;
            _importManager = importManager;
            _events = events;
            _cityManager = cityManager;

            events.Subscribe(this);
            SettingsViewModel = settingsViewModel;
            ParsingViewModel = parsingViewModel;
            ParserSettingViewModel = parserSettingViewModel;
            ConsoleViewModel = consoleViewModel;
            AdvertsViewModel = advertsViewModel;
            BlackListViewModel = blackListViewModel;

            _statusTimer = new Timer {Interval = 5000};
            _statusTimer.Elapsed += _statusTimer_Elapsed;

            Items.Add(parsingViewModel);
            Items.Add(parserSettingViewModel);
            Items.Add(exportSettingsViewModel);
            Items.Add(exportQueueView);
            Items.Add(advertsViewModel);
            Items.Add(phonesViewModel);
            Items.Add(proxiesViewModel);
            Items.Add(citiesViewModel);
            Items.Add(statViewModel);
            Items.Add(rulesView);

            //if (ModeManager.Mode == ReleaseMode.Debug)
            Items.Add(testParsingViewModel);

            ActivateItem(parsingViewModel);

            //init ----------

            DisplayName = "Real Estate 2.0";

            Trace.WriteLine("Start initialization...");

            Trace.WriteLine("Loading settings from file...");
            settingsManager.Initialize();
            Trace.WriteLine("Loading settings from file done");

            _events.Publish(new LoggingEvent());

            Trace.WriteLine("Checking database...");
            CriticalErrorEvent criticalError = null;
            try
            {
                using (var context = new RealEstateContext())
                {
                    if (context.Database.Exists())
                    {
                        if (!context.Database.CompatibleWithModel(false))
                        {
                            context.Database.Initialize(force: true);
                        }
                        //commented for development
                        if (!context.Database.CompatibleWithModel(false))
                        {
                            Trace.WriteLine("Database has non-actual state. Please, update DB structure", "Error");
                            criticalError = new CriticalErrorEvent
                            {
                                Message = "Ошибка базы данных. \r\n База данных в неактуальном состоянии. \r\n Попробуйте перезапустить программу"
                            };
                        }
                    }
                    else
                    {
                        Trace.WriteLine("Database not exist. Creating...");
                        context.Database.CreateIfNotExists();
                    }
                }
            }
            catch (DataException sokex)
            {
                Trace.WriteLine(sokex.ToString());
                criticalError = new CriticalErrorEvent
                {
                    Message = "Ошибка базы данных. \r\n Невозможно подключиться к базе данных. \r\n Проверьте строку подключения"
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent
                {
                    Message = "Ошибка базы данных. \r\n Смотрите лог для подробностей."
                };
            }

            try
            {
                InitProxy(proxyManager);
                InitCity(cityManager);
                InitImportSites(importManager);
                InitRules(rulesManager);
                phoneManager.Restore();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent
                {
                    Message = "Ошибка инициализации файлов данных. \r\n Смотрите лог для подробностей."
                };
            }

            try
            {
                InitExportSites(exportSiteManager);
                InitExportQueue(exportingManager);
                InitOKATODriver(okatoDriver);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent
                {
                    Message = "Ошибка инициализации данных из базы. \r\n Смотрите лог для подробностей."
                };
            }

            if (criticalError == null)
            {
                RealEstateContext.isOk = true;
                Trace.WriteLine("Database is OK");

                Trace.WriteLine("Application initialize done");
            }
            else
                Task.Factory.StartNew(() =>
                {
                    while (!Loader.IsFormLoaded)
                        Thread.Sleep(300);

                    Thread.Sleep(1000);
                    _events.Publish(criticalError);
                });
        }

        public override void CanClose(Action<bool> callback)
        {
            callback(
                MessageBox.Show("Действительно закрыть приложение?", "Внимание", MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes);
        }

        private static void InitOKATODriver(OKATODriver okatoDriver)
        {
            Trace.WriteLine("Loading okato table...");
            OKATODriver.Load();
        }

        private static void InitRules(RulesManager rulesManager)
        {
            Trace.WriteLine("Init rules...");
            rulesManager.Load();
            Trace.WriteLine("Rules parse ok");
        }

        private void InitExportQueue(ExportingManager exportingManager)
        {
            exportingManager.RestoreQueue();
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                try
                {
                    _importManager.Save();
                    _cityManager.Save();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    MessageBox.Show("Ошибка сохранения настрок! \r\n См. лог для подробностей", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            base.OnDeactivate(close);
        }

        private static void InitProxy(ProxyManager proxyManager)
        {
            Trace.WriteLine("Loading proxies...");

            proxyManager.Restore();
        }

        private static void InitCity(CityManager cityManager)
        {
            Trace.WriteLine("Loading cities...");

            cityManager.Restore();
        }

        private void InitImportSites(ImportManager importManager)
        {
            Trace.WriteLine("Loading import sites...");

            importManager.Restore();
        }

        private void InitExportSites(ExportSiteManager exportSiteManager)
        {
            Trace.WriteLine("Loading sites for export...");

            exportSiteManager.Restore();
        }

        public void OpenSettings()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var style = new Dictionary<string, object> {{"style", "VS2012ModalWindowStyle"}};

                    _windowManager.ShowDialog(SettingsViewModel, settings: style);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private bool isToolsOpen = true;

        private Boolean ToogleTools
        {
            get { return isToolsOpen; }
            set
            {
                isToolsOpen = value;
                NotifyOfPropertyChange(() => ToogleTools);
                NotifyOfPropertyChange(() => ToogleToolsTooltip);
                _events.Publish(new ToolsOpenEvent(isToolsOpen));
            }
        }

        private string ToogleToolsTooltip
        {
            get { return ToogleTools ? "Скрыть панель инструментов" : "Показать панель инструментов"; }
        }

        private bool isConsoleOpen;

        public Boolean IsConsoleOpen
        {
            get { return isConsoleOpen; }
            set
            {
                isConsoleOpen = value;
                ConsoleViewModel.IsOpen = value;
                NotifyOfPropertyChange(() => IsConsoleOpen);
            }
        }

        private bool isViewOpen;

        public Boolean IsViewOpen
        {
            get { return isViewOpen; }
            set
            {
                isViewOpen = value;
                BlackListViewModel.IsViewOpen = value;
                NotifyOfPropertyChange(() => IsViewOpen);
            }
        }

        private bool _IsEnabled = true;

        private bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                _IsEnabled = value;
                NotifyOfPropertyChange(() => IsEnabled);
            }
        }

        private string _Status = Ok_Status;

        private string Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                NotifyOfPropertyChange(() => Status);

                if (value != Ok_Status)
                {
                    _statusTimer.Stop();
                    _statusTimer.Start();
                }
                else
                    _statusTimer.Stop();
            }
        }

        public void Handle(string message)
        {
            Status = message;
        }

        private void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Status.Contains("..."))
                Status = Ok_Status;
        }

        private const string Ok_Status = "Готово";


        public void Handle(CriticalErrorEvent message)
        {
            Status = "Критическая ошибка...";
            IsEnabled = false;
            _events.Publish(new ToolsOpenEvent(false));
            MessageBox.Show(message.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class ToolsOpenEvent
    {
        public bool IsOpen { get; private set; }

        public ToolsOpenEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }

    public class CriticalErrorEvent
    {
        public string Message { get; set; }
    }
}