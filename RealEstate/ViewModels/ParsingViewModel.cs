using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Proxies;
using RealEstate.TaskManagers;
using RealEstate.City;
using RealEstate.Parsing;

namespace RealEstate.ViewModels
{
    [Export(typeof(ParsingViewModel))]
    public class ParsingViewModel : ValidatingScreen<ParsingViewModel>, IHandle<ToolsOpenEvent>, IHandle<CriticalErrorEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;
        private readonly CityManager _cityManager;
        private readonly ImportManager _importManager;

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager, CityManager cityManager, ImportManager importManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            events.Subscribe(this);
            DisplayName = "Главная";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            ImportSite = Parsing.ImportSite.All; //if change, do on view too

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

        public BindableCollection<CityWrap> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private ParsePeriod _ParsePeriod = ParsePeriod.Today;
        public ParsePeriod ParsePeriod
        {
            get { return _ParsePeriod; }
            set
            {
                _ParsePeriod = value;
                NotifyOfPropertyChange(() => ParsePeriod);
            }
        }

        public BindableCollection<ParsingSite> ImportSites
        {
            get
            {
                return _importManager.ParsingSites;
            }
        }


        public ImportSite ImportSite { get; set; }


        public void SwitchImportSite(ParsingSite site)
        {
            ImportSite = site.Site;
        }

        private RealEstateType _RealEstateType = RealEstateType.Apartments;
        public RealEstateType RealEstateType
        {
            get { return _RealEstateType; }
            set
            {
                _RealEstateType = value;
                NotifyOfPropertyChange(() => RealEstateType);
            }
        }

        private Usedtype _Usedtype = Usedtype.All;
        public Usedtype Usedtype
        {
            get { return _Usedtype; }
            set
            {
                _Usedtype = value;
                NotifyOfPropertyChange(() => Usedtype);
            }
        }

        private AdvertType _AdvertType = AdvertType.Sell;
        public AdvertType AdvertType
        {
            get { return _AdvertType; }
            set
            {
                _AdvertType = value;
                NotifyOfPropertyChange(() => AdvertType);
            }
        }
    }
}
