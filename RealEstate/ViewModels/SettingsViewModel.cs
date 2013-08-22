using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using RealEstate.Settings;

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : ValidatingScreen<SettingsViewModel>
    {
        protected override void OnActivate()
        {
            base.OnActivate();
            LogFileName = SettingsManager.LogFileName;
            WriteToLog = SettingsManager.LogToFile;
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

        public void SaveGeneral()
        {
            if (WriteToLog != SettingsManager.LogToFile)
            {
                SettingsManager.LogToFile = WriteToLog;
                if (WriteToLog)
                    RealEstate.Log.LogManager.EnableLogToFile(LogFileName);
                else
                    RealEstate.Log.LogManager.DisableLogToFile();
            }

            SettingsManager.LogFileName = LogFileName;

            SettingsManager.Save();
        }


    }
}
