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
    [Export(typeof(EditExportSiteViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class EditExportSiteViewModel : ValidatingScreen<EditExportSiteViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly CityManager _cityManager;

        public ExportSite Site { get; set; }

        [ImportingConstructor]
        public EditExportSiteViewModel(IEventAggregator events, ExportSiteManager exportSiteManager, CityManager citymanager)
        {
            _events = events;
            _exportSiteManager = exportSiteManager;
            _cityManager = citymanager;

            //events.Subscribe(this);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if(Site == null)
                this.DisplayName = "Добавить новый сайт";
            else
            {
                this.DisplayName = "Редактирование сайта";
                FtpName = Site.DisplayName;
                DataBase = Site.Database;
                SelectedCity = _cityManager.Cities.FirstOrDefault(c => c.City == Site.City);
                Ip = Site.Address;
            }
        }

        private string _Title = "";
        [Required]
        public string FtpName
        {
            get { return _Title; }
            set
            {
                _Title = value;
                NotifyOfPropertyChange(() => FtpName);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _Url = "";
        [Required]
        public string DataBase
        {
            get { return _Url; }
            set
            {
                _Url = value;
                NotifyOfPropertyChange(() => DataBase);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _ConectionString = "88.212.209.125";
        [Required]
        public string Ip
        {
            get { return _ConectionString; }
            set
            {
                _ConectionString = value;
                NotifyOfPropertyChange(() => Ip);
                NotifyOfPropertyChange(() => CanSave);
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
        public void Save()
        {
            if (!HasErrors)
            {

                bool isNew = false;
                if (Site == null)
                {
                    isNew = true;
                    Site = new ExportSite();
                }

                Site.DisplayName = FtpName;
                Site.Database = DataBase;
                Site.City = SelectedCity.City;
                Site.Address = Ip;

                _exportSiteManager.Save(Site);
                if (isNew)
                    _events.Publish("Новый сайт для экспорта добавлен");
                else
                    _events.Publish("Сохранено");

                TryClose();
            }
        }

        public bool CanSave
        {
            get
            {
                return !HasErrors;
            }
        }


    }
}
