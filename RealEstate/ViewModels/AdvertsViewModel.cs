using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using RealEstate.Exporting;

namespace RealEstate.ViewModels
{
    [Export(typeof(AdvertsViewModel))]
    public class AdvertsViewModel : ValidatingScreen<AdvertsViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly IWindowManager _windowManager;
        private readonly ParserSettingManager _parserSettingManager;
        private readonly CityManager _cityManager;
        private readonly RealEstateContext _context;
        private readonly ExportingManager _exportingManager;
        private readonly AdvertsManager _advertsManager;

        [ImportingConstructor]
        public AdvertsViewModel(IEventAggregator events, IWindowManager windowManager, ParserSettingManager parserSettingManager,
            CityManager cityManager, RealEstateContext context, ExportingManager exportingManager, AdvertsManager advertsManager)
        {
            _events = events;
            _windowManager = windowManager;
            _parserSettingManager = parserSettingManager;
            _cityManager = cityManager;
            _context = context;
            _exportingManager = exportingManager;
            _advertsManager = advertsManager;
            events.Subscribe(this);
            DisplayName = "Объявления";
        }

        protected override void OnInitialize()
        {
            SelectedCity = Cities.FirstOrDefault();
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

                Usedtype = Usedtype.All;
                NotifyOfPropertyChange(() => UsedTypes);

            }
        }

        private BindableCollection<UsedTypeNamed> _usedTypes = new BindableCollection<UsedTypeNamed>();
        public BindableCollection<UsedTypeNamed> UsedTypes
        {
            get
            {
                _usedTypes.Clear();
                _usedTypes.AddRange(_parserSettingManager.SubTypes(RealEstateType));
                return _usedTypes;
            }
        }

        public void ChangeSubtype(UsedTypeNamed type)
        {
            Usedtype = type.Type;
        }

        public Usedtype Usedtype { get; set; }

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

        private ImportSite _ImportSite = ImportSite.Avito;
        public ImportSite ImportSite
        {
            get { return _ImportSite; }
            set
            {
                _ImportSite = value;
                NotifyOfPropertyChange(() => ImportSite);
            }
        }

        private DateTime _Start = DateTime.Now;
        public DateTime Start
        {
            get { return _Start; }
            set
            {
                _Start = value;
                NotifyOfPropertyChange(() => Start);
            }
        }

        private DateTime _Final = DateTime.Now;
        public DateTime Final
        {
            get { return _Final; }
            set
            {
                _Final = value;
                NotifyOfPropertyChange(() => Final);
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

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        public void Search()
        {
            try
            {

                _Adverts.Clear();
                DateTime start = DateTime.MinValue;
                DateTime final = DateTime.MaxValue;

                if (ParsePeriod != ParsePeriod.Custom)
                {
                    start = ParserSetting.GetDate(ParsePeriod);
                }
                else
                {
                    start = Start;
                    final = Final;
                }

                bool citySearch = SelectedCity.City != CityWrap.ALL;
                bool importSearch = ImportSite != ImportSite.All;
                bool realSearch = RealEstateType != RealEstateType.All;
                bool usedSearch = Usedtype != Usedtype.All;
                bool advertSearch = AdvertType != AdvertType.All;
                bool dateSearch = ParsePeriod != ParsePeriod.All;
                bool lastParsing = OnlyLastParsing;

                Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                if (_context.Adverts.FirstOrDefault() == null) return;

                                var adverts = from a in _context.Adverts
                                              where
                                                 (citySearch ? a.City == SelectedCity.City : true)
                                              && (importSearch ? a.ImportSiteValue == (int)ImportSite : true)
                                              && (realSearch ? a.RealEstateTypeValue == (int)RealEstateType : true)
                                              && (usedSearch ? a.UsedtypeValue == (int)Usedtype : true)
                                              && (advertSearch ? a.AdvertTypeValue == (int)AdvertType : true)
                                              && (dateSearch ? (a.DateSite <= final && a.DateSite >= start) : true)
                                              && (lastParsing ? a.ParsingNumber == _advertsManager.LastParsingNumber : true)
                                              orderby a.DateSite descending
                                              select a;

                                var byUnique = _advertsManager.Filter(adverts.ToList(), Unique);
                                var filtered = _exportingManager.Filter(byUnique, ExportStatus);
                                _Adverts.AddRange(filtered);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex.ToString());
                                _events.Publish("Ошибка");
                            }
                        });

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                _events.Publish("Ошибка");
            }
        }


        private BindableCollection<Advert> _Adverts = new BindableCollection<Advert>();
        public BindableCollection<Advert> Adverts
        {
            get { return _Adverts; }
            set
            {
                _Adverts = value;
                NotifyOfPropertyChange(() => Adverts);
            }
        }

        public void OpenUrl(Advert advert)
        {

            Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            Process.Start(advert.Url);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString());
                            _events.Publish("Ошибка");
                        }
                    }, CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskScheduler.Default);
        }

        public void Edit(Advert advert)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var style = new Dictionary<string, object>();
                    style.Add("style", "VS2012ModalWindowStyle");

                    var model = IoC.Get<AdvertViewModel>();
                    model.AdvertOriginal = advert;
                    _windowManager.ShowDialog(model, settings: style);

                    Search();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        
        private UniqueEnum _Unique = UniqueEnum.All;
        public UniqueEnum Unique
        {
            get { return _Unique; }
            set
            {
                _Unique = value;
                NotifyOfPropertyChange(() => Unique);
            }
        }

        
        private ExportStatus _ExportStatus = ExportStatus.Unprocessed;
        public ExportStatus ExportStatus
        {
            get { return _ExportStatus; }
            set
            {
                _ExportStatus = value;
                NotifyOfPropertyChange(() => ExportStatus);
            }
        }

        
        private bool _OnlyLastParsing;
        public bool OnlyLastParsing
        {
            get { return _OnlyLastParsing; }
            set
            {
                _OnlyLastParsing = value;
                NotifyOfPropertyChange(() => OnlyLastParsing);
            }
        }
                     
    }
}
