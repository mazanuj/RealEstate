using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;

namespace RealEstate.ViewModels
{
    [Export(typeof(ProxiesViewModel))]
    public class ProxiesViewModel : Screen
    {
        private readonly IEventAggregator _events;

        [ImportingConstructor]
        public ProxiesViewModel(IEventAggregator events)
        {
            _events = events;
            DisplayName = "Прокси";
            IsEnabled = true;
        }

        public bool IsEnabled { get; set; }
    }
}
