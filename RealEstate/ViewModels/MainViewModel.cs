using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;
using System.Diagnostics;
using RealEstate.Settings;
using System.Threading.Tasks;
using RealEstate.Log;
using System.Timers;
using RealEstate.Db;
using System.Data.Entity;
using System.Net.Sockets;
using System.Threading;
using RealEstate.Proxies;
using RealEstate.City;
using RealEstate.Exporting;
using RealEstate.Parsing;

namespace RealEstate.ViewModels
{
    [Export(typeof(MainViewModel))]
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<string>, IHandle<CriticalErrorEvent>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _events;
        private readonly Log.LogManager _logManager;
        private readonly ProxyManager _proxyManager;
        private readonly CityManager _cityManager;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly SettingsManager _settingsManager;
        private readonly ImportManager _importManager;
        private readonly System.Timers.Timer _statusTimer;
        public ConsoleViewModel ConsoleViewModel;
        public SettingsViewModel SettingsViewModel;
        public ParsingViewModel ParsingViewModel;
        public ParserSettingViewModel ParserSettingViewModel;

        [ImportingConstructor]
        public MainViewModel(IWindowManager windowManager, IEventAggregator events,
            ProxyManager proxyManager, CityManager cityManager, ExportSiteManager exportSiteManager,
            ConsoleViewModel consoleViewModel, Log.LogManager logManager, SettingsManager settingsManager,
            ImportManager importManager,
            SettingsViewModel settingsViewModel, ProxiesViewModel proxiesViewModel,
            ParsingViewModel parsingViewModel, ParserSettingViewModel parserSettingViewModel)
        {
            _windowManager = windowManager;
            this.ConsoleViewModel = consoleViewModel;
            _logManager = logManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _exportSiteManager = exportSiteManager;
            _importManager = importManager;
            _events = events;
            events.Subscribe(this);
            _settingsManager = settingsManager;
            SettingsViewModel = settingsViewModel;
            ParsingViewModel = parsingViewModel;
            ParserSettingViewModel = parserSettingViewModel;

            _statusTimer = new System.Timers.Timer();
            _statusTimer.Interval = 5000;
            _statusTimer.Elapsed += _statusTimer_Elapsed;

            Items.Add(parsingViewModel);
            Items.Add(proxiesViewModel);
            Items.Add(parserSettingViewModel);

            ActivateItem(parsingViewModel);

            //init ----------

            this.DisplayName = "Real Estate 2.0";

            Trace.WriteLine("Start initialization...");

            Trace.WriteLine("Loading settings from file...");
            _settingsManager.Initialize();
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
                        //commented for development
                        //if (!context.Database.CompatibleWithModel(false))
                        //{
                        //    Trace.WriteLine("Database has non-actual state. Please, update DB structure", "Error");
                        //    dbError = new CriticalErrorEvent() { Message = "Ошибка базы данных. \r\n База данных в неактуальном состоянии. \r\n Обратитесь к программисту" };
                        //}
                    }
                    else
                    {
                        Trace.WriteLine("Database not exist. Creating...");
                        context.Database.CreateIfNotExists();
                    }
                }
            }
            catch (System.Data.DataException sokex)
            {
                Trace.WriteLine(sokex.ToString());
                criticalError = new CriticalErrorEvent() { Message = "Ошибка базы данных. \r\n Невозможно подключиться к базе данных. \r\n Проверьте строку подключения" };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent() { Message = "Ошибка базы данных. \r\n Смотрите лог для подробностей." };
            }

            try
            {
                InitProxy();
                InitCity();
                InitImportSites();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent() { Message = "Ошибка инициализации файлов данных. \r\n Смотрите лог для подробностей." };
            }

            try
            {
                InitExportSites();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                criticalError = new CriticalErrorEvent() { Message = "Ошибка инициализации данных из базы. \r\n Смотрите лог для подробностей." };
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
                    while (!RealEstate.Views.Loader.IsFormLoaded)
                        Thread.Sleep(300);

                    Thread.Sleep(500);
                    _events.Publish(criticalError);
                });
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
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    MessageBox.Show("Ошибка сохранения настрок! \r\n См. лог для подробностей", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            base.OnDeactivate(close);
        }

        private void InitProxy()
        {
            Trace.WriteLine("Loading proxies...");

            _proxyManager.Restore();
        }

        private void InitCity()
        {
            Trace.WriteLine("Loading cities...");

            _cityManager.Restore();
        }

        private void InitImportSites()
        {
            Trace.WriteLine("Loading import sites...");

            _importManager.Restore();
        }

        private void InitExportSites()
        {
            Trace.WriteLine("Loading sites for export...");

            _exportSiteManager.Restore();
        }

        public void OpenSettings()
        {
            try
            {
                var style = new Dictionary<string, object>();
                style.Add("style", "VS2012ModalWindowStyle");

                _windowManager.ShowDialog(SettingsViewModel, settings: style);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
            }
        }

        private bool isToolsOpen = true;
        public Boolean ToogleTools
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

        public string ToogleToolsTooltip
        {
            get
            {
                if (ToogleTools)
                    return "Скрыть панель инструментов";
                else
                    return "Показать панель инструментов";
            }
        }

        private bool isConsoleOpen = false;
        public Boolean IsConsoleOpen
        {
            get { return isConsoleOpen; }
            set
            {
                isConsoleOpen = value;
                NotifyOfPropertyChange(() => IsConsoleOpen);
            }
        }

        private bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                _IsEnabled = value;
                NotifyOfPropertyChange(() => IsEnabled);
            }
        }

        private string _Status = Ok_Status;
        public string Status
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

        void _statusTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Status.Contains("..."))
                Status = Ok_Status;
        }

        const string Ok_Status = "Готово";


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
        public bool IsOpen { get; set; }
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
