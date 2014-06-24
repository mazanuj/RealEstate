using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Net;
using Caliburn.Micro;
using RealEstate.Exporting;
using RealEstate.City;
using RealEstate.Validation;

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
                DisplayName = "Добавить новый сайт";
            else
            {
                DisplayName = "Редактирование сайта";

                SelectedCity = _cityManager.Cities.FirstOrDefault(c => c.City == Site.City);
                Ip = Site.Ip;
                DataBaseUserName = Site.DatabaseUserName;
                DataBasePassword = Site.DatabasePassword;
                DataBase = Site.Database;
                FtpUserName = Site.FtpUserName;
                FtpPassword = Site.FtpPassword;
                FtpFolder = Site.FtpFolder;
                Title = Site.Title;
            }
        }

        private string _Title = "";
        [Required]
        public string Title
        {
            get { return _Title; }
            set
            {
                _Title = value;
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _FtpFolder = "";
        public string FtpFolder
        {
            get { return _FtpFolder; }
            set
            {
                _FtpFolder = value;
                NotifyOfPropertyChange(() => FtpFolder);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _FtpUserName = "";
        [Required]
        public string FtpUserName
        {
            get { return _FtpUserName; }
            set
            {
                _FtpUserName = value;
                NotifyOfPropertyChange(() => FtpUserName);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _FtpPassword = "";
        [Required]
        public string FtpPassword
        {
            get { return _FtpPassword; }
            set
            {
                _FtpPassword = value;
                NotifyOfPropertyChange(() => FtpPassword);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _Db = "";
        [Required]
        public string DataBase
        {
            get { return _Db; }
            set
            {
                _Db = value;
                NotifyOfPropertyChange(() => DataBase);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _DataBaseUserName = "";
        [Required]
        public string DataBaseUserName
        {
            get { return _DataBaseUserName; }
            set
            {
                _DataBaseUserName = value;
                NotifyOfPropertyChange(() => DataBaseUserName);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        private string _DataBasePassword = "";
        [Required]
        public string DataBasePassword
        {
            get { return _DataBasePassword; }
            set
            {
                _DataBasePassword = value;
                NotifyOfPropertyChange(() => DataBasePassword);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        public Brush RectangleFtpPass
        {
            set
            {
                
            }
        }

        private string _Server = "88.212.209.125";
        [Required]
        public string Ip
        {
            get { return _Server; }
            set
            {
                _Server = value;
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

        private CityWrap _selectedCity;
        public CityWrap SelectedCity
        {
            get { return _selectedCity; }
            set
            {
                _selectedCity = value;
                NotifyOfPropertyChange(() => SelectedCity);
            }
        }

        public void SiteValidate()
        {
            var ftpConnect = (FtpWebRequest) WebRequest.Create("ftp://" + Ip);
            ftpConnect.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            ftpConnect.Credentials = new NetworkCredential(FtpUserName, FtpPassword);
            var response = (FtpWebResponse)ftpConnect.GetResponse();
            if (response.WelcomeMessage.StartsWith("230"))
            {
                                
            }

        }
        public void Save()
        {
            if (HasErrors) return;
            var isNew = false;
            if (Site == null)
            {
                isNew = true;
                Site = new ExportSite();
            }

            Site.Title = Title;
            Site.FtpFolder = FtpFolder;
            Site.FtpPassword = FtpPassword;
            Site.FtpUserName = FtpUserName;
            Site.Database = DataBase;
            Site.DatabasePassword = DataBasePassword;
            Site.DatabaseUserName = DataBaseUserName;
            Site.City = SelectedCity.City;
            Site.Ip = Ip;

            _exportSiteManager.Save(Site);
            _events.Publish(isNew ? "Новый сайт для экспорта добавлен" : "Сохранено");

            TryClose();
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
