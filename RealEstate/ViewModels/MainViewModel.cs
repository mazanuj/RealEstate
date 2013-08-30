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

namespace RealEstate.ViewModels
{
    [Export(typeof(MainViewModel))]
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive, IHandle<string>
    {
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _events;
        private readonly Log.LogManager _logManager;
        private readonly SettingsManager _settingsManager;
        private readonly Timer _statusTimer;
        public ConsoleViewModel ConsoleViewModel;
        public SettingsViewModel SettingsViewModel;

        [ImportingConstructor]
        public MainViewModel(IWindowManager windowManager, IEventAggregator events,
            ConsoleViewModel consoleViewModel, Log.LogManager logManager, SettingsManager settingsManager,
            SettingsViewModel settingsViewModel, ProxiesViewModel proxiesViewModel)
        {
            _windowManager = windowManager;
            this.ConsoleViewModel = consoleViewModel;
            _logManager = logManager;
            _events = events;
            events.Subscribe(this);
            _settingsManager = settingsManager;
            SettingsViewModel = settingsViewModel;

            _statusTimer = new Timer();
            _statusTimer.Interval = 5000;
            _statusTimer.Elapsed += _statusTimer_Elapsed;

            Items.Add(proxiesViewModel);

            ActivateItem(proxiesViewModel);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Real Estate 2.0";

            Trace.WriteLine("Start initialization...");

            Trace.WriteLine("Loading settings from file...");
            _settingsManager.Initialize();
            Trace.WriteLine("Loading settings from file done");

            _events.Publish(new LoggingEvent());

            Trace.WriteLine("Application initialize done");
        }

        public void OpenSettings()
        {
            var style = new Dictionary<string, object>();
            style.Add("style", "VS2012ModalWindowStyle");

            _windowManager.ShowDialog(SettingsViewModel, settings: style);
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

        private string _Status = "";
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

    }

    public class ToolsOpenEvent
    {
        public bool IsOpen { get; set; }
        public ToolsOpenEvent(bool isOpen)
        {
            IsOpen = isOpen;
        }
    }
}
