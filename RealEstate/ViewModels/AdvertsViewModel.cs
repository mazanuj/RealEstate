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
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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

        [ImportingConstructor]
        public AdvertsViewModel(IEventAggregator events, IWindowManager windowManager, ParserSettingManager parserSettingManager,
            CityManager cityManager, RealEstateContext context)
        {
            _events = events;
            _windowManager = windowManager;
            _parserSettingManager = parserSettingManager;
            _cityManager = cityManager;
            _context = context;
            events.Subscribe(this);
            DisplayName = "Объявления";
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

                Usedtype = Parsing.Usedtype.All;
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

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        public void Search()
        {
            Task.Factory.StartNew(() =>
                    {
                        _Adverts.Clear();
                        _Adverts.AddRange(_context.Adverts.ToList());
                    });
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
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

    }
}
