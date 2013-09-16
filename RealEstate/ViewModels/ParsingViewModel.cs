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
using RealEstate.Utils;
using System.Threading.Tasks;

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
        private readonly ParserSettingManager _parserSettingManager;

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager,
            CityManager cityManager, ImportManager importManager, ParserSettingManager parserSettingManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            _parserSettingManager = parserSettingManager;
            events.Subscribe(this);
            DisplayName = "Главная";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            ImportSite = Parsing.ImportSite.All; //if change, do on view too
            Usedtype = Parsing.Usedtype.All;
            SelectedCity = _cityManager.Cities.FirstOrDefault();

        }

        public void Handle(CriticalErrorEvent message)
        {
            //if (cs != null)
            //    cs.Cancel();
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


        private CityWrap _SelectedCity = null;
        public CityWrap SelectedCity
        {
            get { return _SelectedCity; }
            set
            {
                _SelectedCity = value;
                NotifyOfPropertyChange(() => SelectedCity);
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


        private bool _UseProxy = true;
        public bool UseProxy
        {
            get { return _UseProxy; }
            set
            {
                _UseProxy = value;
                NotifyOfPropertyChange(() => UseProxy);
            }
        }

        public BindableCollection<ParsingTask> _tasks = new BindableCollection<ParsingTask>();
        public BindableCollection<ParsingTask> Tasks
        {
            get
            {
                return _tasks;
            }
        }

        public void Start()
        {
            if (this.ImportSite == Parsing.ImportSite.All)
            {
                foreach (var site in Enum.GetValues(typeof(ImportSite)))
                {
                    var s = (ImportSite)site;

                    if (s != Parsing.ImportSite.All)
                    {
                        TaskParsingParams param = new TaskParsingParams();

                        param.city = SelectedCity.City;
                        param.period = this.ParsePeriod;
                        param.site = s;
                        param.realType = this.RealEstateType;
                        param.subType = this.Usedtype;
                        param.advertType = this.AdvertType;
                        param.useProxy = UseProxy;

                        ParsingTask realTask = new ParsingTask();
                        realTask.Description = s.ToString(); //todo add name mapper
                        realTask.Task = new Task(() => StartInternal(param, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                        Tasks.Add(realTask);
                        _taskManager.AddTask(realTask);
                    }
                }
            }
            else
            {
                TaskParsingParams param = new TaskParsingParams();

                param.city = SelectedCity.City;
                param.period = this.ParsePeriod;
                param.site = this.ImportSite;
                param.realType = this.RealEstateType;
                param.subType = this.Usedtype;
                param.advertType = this.AdvertType;
                param.useProxy = UseProxy;

                ParsingTask realTask = new ParsingTask();
                realTask.Description = param.site.ToString();
                realTask.Task = new Task(() => StartInternal(param, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                Tasks.Add(realTask);
                _taskManager.AddTask(realTask);
            }
        }

        private void StartInternal(TaskParsingParams param, CancellationToken ct, PauseToken pt, ParsingTask task)
        {
            var settings = _parserSettingManager.FindSettings(param.advertType, param.city,
                param.site, param.period, param.realType, param.subType);



            Thread.Sleep(1000);
        }



    }

    class TaskParsingParams
    {
        public string city;
        public ParsePeriod period;
        public ImportSite site;
        public RealEstateType realType;
        public Usedtype subType;
        public AdvertType advertType;
        public bool useProxy;
    }

    public class ParsingTask : RealEstateTask
    {
        
        private int _TotlaCount = 0;
        public int TotlaCount
        {
            get { return _TotlaCount; }
            set
            {
                _TotlaCount = value;
                NotifyOfPropertyChange(() => TotlaCount);
            }
        }

        
        private int _ParsedCount = 0;
        public int ParsedCount
        {
            get { return _ParsedCount; }
            set
            {
                _ParsedCount = value;
                NotifyOfPropertyChange(() => ParsedCount);
            }
        }

        
        private TimeSpan _PassBy = TimeSpan.MinValue;
        public TimeSpan PassBy
        {
            get { return _PassBy; }
            set
            {
                _PassBy = value;
                NotifyOfPropertyChange(() => PassBy);
            }
        }

        
        private TimeSpan _Remaining = TimeSpan.MinValue;
        public TimeSpan Remaining
        {
            get { return _Remaining; }
            set
            {
                _Remaining = value;
                NotifyOfPropertyChange(() => Remaining);
            }
        }
                    
                    
                    
                    
    }
}
