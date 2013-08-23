using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using RealEstate.Settings;
using System.Threading.Tasks;
using System.Diagnostics;
using RealEstate.Log;

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : ValidatingScreen<SettingsViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly SettingsManager _settingsManager;

        [ImportingConstructor]
        public SettingsViewModel(IEventAggregator events, SettingsManager settingsManager)
        {
            _events = events;
            _settingsManager = settingsManager;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Настройки";
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            LogFileName = SettingsStore.LogFileName;
            WriteToLog = SettingsStore.LogToFile;
        }

        private string _LogFileName = "log.txt";
        [Required(ErrorMessage = "Email is required")]
        [RegularExpression(".+\\..+")]
        public string LogFileName
        {
            get { return _LogFileName; }
            set
            {
                _LogFileName = value;
                NotifyOfPropertyChange(() => LogFileName);
            }
        }


        private bool _WriteToLog = false;
        public bool WriteToLog
        {
            get { return _WriteToLog; }
            set
            {
                _WriteToLog = value;
                NotifyOfPropertyChange(() => WriteToLog);
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
            }
        }
                    

        public async void SaveGeneral()
        {
            Status = "Сохраняю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    bool changed = false;
                    if (WriteToLog != SettingsStore.LogToFile || LogFileName != SettingsStore.LogFileName)
                    {
                        SettingsStore.LogToFile = WriteToLog;
                        SettingsStore.LogFileName = LogFileName;

                        _events.Publish(new LoggingEvent());

                        changed = true;
                    }

                    if(changed)
                        _settingsManager.Save();
                });

                Status = "Сохранено";

            }
            catch (Exception ex)
            {
                Status = "Ошибка";
                Trace.WriteLine(ex.ToString());
            }
        }

    }
}
