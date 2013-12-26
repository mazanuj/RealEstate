using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Db;
using RealEstate.Exporting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(ExportQueueViewModel))]
    public class ExportQueueViewModel : ValidatingScreen<ExportQueueViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly RealEstateContext _context;
        private readonly IEventAggregator _events;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly ExportingManager _exportingManager;

        [ImportingConstructor]
        public ExportQueueViewModel(IEventAggregator events, RealEstateContext context,
            ExportSiteManager exportSiteManager, ExportingManager exportingManager)
        {
            _exportingManager = exportingManager;
            _exportSiteManager = exportSiteManager;
            _context = context;
            _events = events;
            events.Subscribe(this);
            DisplayName = "Очередь экспорта";
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }


        protected override void OnActivate()
        {
            base.OnActivate();
            if (!RealEstate.Db.RealEstateContext.isOk) return;
        }

        public ObservableQueue<ExportItem> Items
        {
            get { return _exportingManager.ExportQueue; }
        }
    }
}
