using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using RealEstate.TaskManagers;
using RealEstate.Parsing;
using System.Windows;
using RealEstate.City;
using RealEstate.Exporting;

namespace RealEstate.ViewModels
{
    [Export(typeof(ParserSettingViewModel))]
    public class ParserSettingViewModel : ValidatingScreen<ParserSettingViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly CityManager _cityManager;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly IWindowManager _windowManager;
        private readonly AddExportSiteViewModel _addExportSiteViewModel;

        [ImportingConstructor]
        public ParserSettingViewModel(IWindowManager windowManager, IEventAggregator events, 
            TaskManager taskManager, ExportSiteManager exportSiteManager, CityManager cityManager,
            AddExportSiteViewModel addExportSiteViewModel)
        {
            _events = events;
            _windowManager = windowManager;
            _taskManager = taskManager;
            _cityManager = cityManager;
            _exportSiteManager = exportSiteManager;
            _addExportSiteViewModel = addExportSiteViewModel;

            events.Subscribe(this);
            DisplayName = "Настройки проекта";
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            SelectedCitie = _cityManager.Cities.First();
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
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

        private RealEstateType _RealEstateType = RealEstateType.Apartments;
        public RealEstateType RealEstateType
        {
            get { return _RealEstateType; }
            set
            {
                _RealEstateType = value;
                NotifyOfPropertyChange(() => RealEstateType);
                FilterValuesChanged();
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
                FilterValuesChanged();
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
                FilterValuesChanged();
            }
        }

        private ImportSite _ImportSite = ImportSite.Avito;
        public ImportSite ImportSite
        {
            get { return _ImportSite; }
            set
            {
                _ImportSite = value;
                NotifyOfPropertyChange(() => ImportSite);
                FilterValuesChanged();
            }
        }

        public BindableCollection<string> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private string _SelectedCitie = null;
        public string SelectedCitie
        {
            get { return _SelectedCitie; }
            set
            {
                _SelectedCitie = value;
                NotifyOfPropertyChange(() => SelectedCitie);
                FilterValuesChanged();
            }
        }


        private void FilterValuesChanged()
        {

        }

        public BindableCollection<ExportSite> ExportSites
        {
            get
            {
                return _exportSiteManager.ExportSites;
            }
        }

        public void AddExportSite()
        {
            var style = new Dictionary<string, object>();
            style.Add("style", "VS2012ModalWindowStyle");

            _windowManager.ShowDialog(_addExportSiteViewModel, settings: style);        
        }

        public void DeleteExportSite(ExportSite site)
        {
            if (site != null)
            {
                if (MessageBox.Show(String.Format("Точно удалить настройки для сайта {0}?", site.DisplayName), "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {

                } 
            }
        }

    }
}
