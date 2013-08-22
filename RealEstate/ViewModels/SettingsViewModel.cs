using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : ValidatingScreen<SettingsViewModel>
    {
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
                    
                    
    }
}
