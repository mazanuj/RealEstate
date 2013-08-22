using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;
using System.Diagnostics;

namespace RealEstate.ViewModels
{
    [Export(typeof(MainViewModel))]
    public class MainViewModel : Screen
    {
        private readonly IWindowManager _windowManager;
        public ConsoleViewModel ConsoleViewModel;

        [ImportingConstructor]
        public MainViewModel(IWindowManager windowManager, ConsoleViewModel consoleViewModel)
        {
            _windowManager = windowManager;
            this.ConsoleViewModel = consoleViewModel;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.WriteLine("Application initialize done");
        }

        public void OpenSettings()
        {
            var style = new Dictionary<string, object>();
            style.Add("style", "VS2012ModalWindowStyle");

            _windowManager.ShowDialog(new SettingsViewModel(), settings: style);
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
    }
}
