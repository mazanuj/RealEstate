using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(StatisticsViewModel))]
    public class StatisticsViewModel : ValidatingScreen<AdvertViewModel>, IHandle<ToolsOpenEvent>
    {
        public StatisticsViewModel()
        {
            this.DisplayName = "Статистика";
        }

        private ImportSite _ImportSite = ImportSite.Avito;
        public ImportSite ImportSite
        {
            get { return _ImportSite; }
            set
            {
                _ImportSite = value;
                NotifyOfPropertyChange(() => ImportSite);
            }
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }
    }
}
