using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;
using Caliburn.Micro.Validation;
using System.Threading.Tasks;
using System.Threading;
using RealEstate.TaskManagers;
using KTF.Proxy.Readers;
using RealEstate.Proxies;
using System.Collections.ObjectModel;
using System.Diagnostics;
using RealEstate.SmartProcessing;

namespace RealEstate.ViewModels
{
    [Export(typeof(RulesViewModel))]
    public class RulesViewModel : ValidatingScreen<RulesViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly RulesManager _rulesManager;
        [ImportingConstructor]
        public RulesViewModel(RulesManager manager)
        {
            _rulesManager = manager;
            DisplayName = "Обработка объявлений";
        }

        public ObservableCollection<Rule> Rules
        {
            get { return _rulesManager.Rules; }
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }


    }
}
