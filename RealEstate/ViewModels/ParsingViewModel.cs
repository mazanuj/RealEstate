using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Proxies;
using RealEstate.TaskManagers;

namespace RealEstate.ViewModels
{
    [Export(typeof(ParsingViewModel))]
    public class ParsingViewModel : ValidatingScreen<ParsingViewModel>, IHandle<ToolsOpenEvent>, IHandle<CriticalErrorEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            events.Subscribe(this);
            DisplayName = "Главная";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

        }

        CancellationTokenSource cs;

        public void Handle(CriticalErrorEvent message)
        {
            if (cs != null)
                cs.Cancel();
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }
    }
}
