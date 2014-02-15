using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Db;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(ExportSettingsViewModel))]
    public class ExportSettingsViewModel : ValidatingScreen<ExportSettingsViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly RealEstateContext _context;
        private readonly CityManager _cityManager;
        private readonly IEventAggregator _events;
        private readonly ParserSettingManager _parserSettingManager;
        private readonly ExportSiteManager _exportSiteManager;

        const string DefaultCity = "";
        const bool DefaultReplacePhone = false;
        const int DefaultDelay = 0;
        const float DefaultMargin = 0;
        const AdvertType DefaultAdvertType = AdvertType.Sell;
        const RealEstateType DefaultRealEstateType = RealEstateType.Apartments;
        const Usedtype DefaulUsedtype = Usedtype.All;

        [ImportingConstructor]
        public ExportSettingsViewModel(IEventAggregator events, RealEstateContext context, CityManager cityManager, ParserSettingManager parserSettingManager
            , ExportSiteManager exportSiteManager)
        {
            _cityManager = cityManager;
            _parserSettingManager = parserSettingManager;
            _context = context;
            _exportSiteManager = exportSiteManager;
            _events = events;
            events.Subscribe(this);
            DisplayName = "Настройки экспорта";
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            SelectedCity = _cityManager.Cities.FirstOrDefault();
            Usedtype = Parsing.Usedtype.All;
        }

        private RealEstateType _RealEstateType = RealEstateType.Apartments;
        public RealEstateType RealEstateType
        {
            get { return _RealEstateType; }
            set
            {
                _RealEstateType = value;
                NotifyOfPropertyChange(() => RealEstateType);
                NotifyOfPropertyChange(() => UsedTypes);
            }
        }

        private BindableCollection<UsedTypeNamed> _usedTypes = new BindableCollection<UsedTypeNamed>();
        public BindableCollection<UsedTypeNamed> UsedTypes
        {
            get
            {
                _usedTypes.Clear();
                var types = _parserSettingManager.SubTypes(RealEstateType);
                types.First(t => t.Type == Usedtype).IsChecked = true;
                _usedTypes.AddRange(types);

                return _usedTypes;
            }
        }

        public void ChangeSubtype(UsedTypeNamed type)
        {
            Usedtype = type.Type;
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

        private bool _ReplacePhoneNumber = false;
        public bool ReplacePhoneNumber
        {
            get { return _ReplacePhoneNumber; }
            set
            {
                _ReplacePhoneNumber = value;
                NotifyOfPropertyChange(() => ReplacePhoneNumber);
            }
        }


        private float _Margin = 0;
        [Range(0, 100)]
        public float MoneyMargin
        {
            get { return _Margin; }
            set
            {
                _Margin = value;
                NotifyOfPropertyChange(() => MoneyMargin);
            }
        }


        private int _Delay = 0;
        [Range(0, 1000)]
        public int Delay
        {
            get { return _Delay; }
            set
            {
                _Delay = value;
                NotifyOfPropertyChange(() => Delay);
            }
        }

        public BindableCollection<ExportSite> ExportSites
        {
            get
            {
                return _exportSiteManager.ExportSites;
            }
        }

        private ExportSite _ExportSite = null;
        public ExportSite SelectedExportSite
        {
            get { return _ExportSite; }
            set
            {
                _ExportSite = value;
                NotifyOfPropertyChange(() => SelectedExportSite);
                NotifyOfPropertyChange(() => CanSave);
            }
        }

        public void ChangeExportSite(ExportSite site)
        {
            SelectedExportSite = site;
            Reload();
        }

        public bool CanSave
        {
            get
            {
                return SelectedExportSite != null;
            }
        }

        public void Save()
        {
            try
            {
                ExportSetting setting = null;

                setting = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == SelectedExportSite.Id);

                if (setting == null)
                {
                    setting = new ExportSetting();
                    _context.ExportSettings.Add(setting);
                }

                setting.AdvertType = AdvertType;
                setting.City = SelectedCity.City;
                setting.Delay = Delay;
                setting.ExportSite = SelectedExportSite;
                setting.Margin = MoneyMargin;
                setting.RealEstateType = RealEstateType;
                setting.ReplacePhoneNumber = ReplacePhoneNumber;
                setting.Usedtype = Usedtype;

                _context.SaveChanges();

                _events.Publish("Сохранено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                _events.Publish("Ошибка сохранения");
            }

        }

        public void Reload()
        {
            try
            {
                if (SelectedExportSite != null)
                {
                    var setting = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == SelectedExportSite.Id);
                    if (setting != null)
                    {
                        AdvertType = setting.AdvertType;
                        SelectedCity = Cities.SingleOrDefault(c => c.City == setting.City);
                        Delay = setting.Delay;
                        MoneyMargin = setting.Margin;
                        Usedtype = setting.Usedtype;
                        RealEstateType = setting.RealEstateType;
                        ReplacePhoneNumber = setting.ReplacePhoneNumber;                        
                    }
                    else
                    {
                        AdvertType = DefaultAdvertType;
                        SelectedCity = Cities.SingleOrDefault(c => c.City == DefaultCity);
                        Delay = DefaultDelay;
                        MoneyMargin = DefaultMargin;
                        Usedtype = DefaulUsedtype;
                        RealEstateType = DefaultRealEstateType;
                        ReplacePhoneNumber = DefaultReplacePhone;                        
                    }
                }
            }
            catch (Exception ex)
            {
                 Trace.WriteLine(ex.ToString());
                _events.Publish("Ошибка загрузки данных");
            }
        }
    }
}
