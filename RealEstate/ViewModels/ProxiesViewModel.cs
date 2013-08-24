using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;
using Caliburn.Micro.Validation;

namespace RealEstate.ViewModels
{
    [Export(typeof(ProxiesViewModel))]
    public class ProxiesViewModel : ValidatingScreen<ProxiesViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;

        [ImportingConstructor]
        public ProxiesViewModel(IEventAggregator events)
        {
            _events = events;
            events.Subscribe(this);
            DisplayName = "Прокси";
            IsEnabled = true;
            IsToolsOpen = true;
        }

        public bool IsEnabled { get; set; }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }
    }
}
