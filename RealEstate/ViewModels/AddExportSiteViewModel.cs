using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Exporting;
using RealEstate.Validation;
using RealEstate.City;

namespace RealEstate.ViewModels
{
    [Export(typeof(AddExportSiteViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AddExportSiteViewModel : ValidatingScreen<AddExportSiteViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly CityManager _cityManager;

        [ImportingConstructor]
        public AddExportSiteViewModel(IEventAggregator events, ExportSiteManager exportSiteManager, CityManager citymanager)
        {
            _events = events;
            _exportSiteManager = exportSiteManager;
            _cityManager = citymanager;

            //events.Subscribe(this);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Добавить новый сайт";
        }

        private string _Title = "";
        [Required(ErrorMessage = "Введите название сайта")]
        public string Name
        {
            get { return _Title; }
            set
            {
                _Title = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => CanCreate);
            }
        }

        private string _Url = "";
        [Required(ErrorMessage = "Введите адрес сайта")]
        [UrlAttribute(ErrorMessage = "Некорректный адрес сайта")]
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                NotifyOfPropertyChange(() => Url);
                NotifyOfPropertyChange(() => CanCreate);
            }
        }

        private string _ConectionString = "";
        [Required(ErrorMessage = "Введите строку подключения к базе")]
        public string ConnectionString
        {
            get { return _ConectionString; }
            set
            {
                _ConectionString = value;
                NotifyOfPropertyChange(() => ConnectionString);
                NotifyOfPropertyChange(() => CanCreate);
            }
        }

        public BindableCollection<CityWrap> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private CityWrap _selectedCity = null;
        public CityWrap SelectedCity
        {
            get { return _selectedCity; }
            set
            {
                _selectedCity = value;
                NotifyOfPropertyChange(() => SelectedCity);
            }
        }
        public void Create()
        {
            if (!HasErrors)
            {
                var site = new ExportSite();
                site.DisplayName = Name;
                site.Database = Url;
                site.City = SelectedCity.City;
                site.Address = ConnectionString;

                _exportSiteManager.Add(site);

                _events.Publish("Новый сайт для экспорта добавлен");
                TryClose();
            }
        }

        public bool CanCreate
        {
            get
            {
                return !HasErrors;
            }
        }


    }
}
