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
using System.Diagnostics;
using RealEstate.Parsing.Parsers;
using System.Net;
using RealEstate.Settings;

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
        private readonly ParsingManager _parsingManager;

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager,
            CityManager cityManager, ImportManager importManager, ParserSettingManager parserSettingManager,
            ParsingManager parsingManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            _parserSettingManager = parserSettingManager;
            _parsingManager = parsingManager;
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
                        param.Delay = ImportSites.First(i => i.Site == s).Delay;
                        param.MaxCount = ImportSites.First(i => i.Site == s).Deep;

                        ParsingTask realTask = new ParsingTask();
                        realTask.Description = _importManager.GetSiteName(s);
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
                param.Delay = ImportSites.First(i => i.Site == this.ImportSite).Delay;
                param.MaxCount = ImportSites.First(i => i.Site == this.ImportSite).Deep;

                ParsingTask realTask = new ParsingTask();
                realTask.Description = _importManager.GetSiteName(param.site);
                realTask.Task = new Task(() => StartInternal(param, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                Tasks.Add(realTask);
                _taskManager.AddTask(realTask);
            }
        }

        private void StartInternal(TaskParsingParams param, CancellationToken ct, PauseToken pt, ParsingTask task)
        {
            try
            {
                if (ct.IsCancellationRequested)
                { _events.Publish("Отменено"); return; }
                if (pt.IsPauseRequested)
                    pt.WaitUntillPaused();

                var settings = _parserSettingManager.FindSettings(param.advertType, param.city,
                    param.site, param.period, param.realType, param.subType);

                int maxattempt = SettingsStore.MaxParsingAttemptCount;

                WebProxy proxy = param.useProxy ? _proxyManager.GetNextProxy() : null;
                var headers = _parsingManager.LoadHeaders(param, settings, ct, pt, proxy, maxattempt);

                task.TotlaCount = headers.Count;

                List<Advert> adverts = new List<Advert>();
                ParserBase parser = ParsersFactory.GetParser(param.site);

                int attempt = 0;
                
                for (int i = 0; i < headers.Count; i++)
                {
                    Advert advert = null;
                    attempt = 0;
                    while (attempt++ < maxattempt)
                    {
                        if (ct.IsCancellationRequested)
                        { _events.Publish("Отменено"); return; }
                        if (pt.IsPauseRequested)
                            pt.WaitUntillPaused();

                        Thread.Sleep(param.Delay * 1000);

                        proxy = param.useProxy ? _proxyManager.GetNextProxy() : null;
                        try
                        {
                            advert = parser.Parse(headers[i], proxy, ct, pt);
                            break;
                        }
                        catch (System.Web.HttpException ex)
                        {
                            Trace.WriteLine(ex.Message, "Http error");
                            _proxyManager.RejectProxy(proxy);
                        }
                        catch (System.Net.WebException wex)
                        {
                            Trace.WriteLine(headers[i].Url);
                            Trace.WriteLine(wex.Message, "Web error");
                            _proxyManager.RejectProxy(proxy);
                        }
                        catch (System.IO.IOException iex)
                        {
                            Trace.WriteLine(headers[i].Url);
                            Trace.WriteLine(iex.Message, "IO error");
                            _proxyManager.RejectProxy(proxy);
                        }
                        catch (ParsingException pex)
                        {
                            Trace.WriteLine(pex.Message + ": " + pex.UnrecognizedData, "Unrecognized data");
                            break;
                        }
                        catch (OperationCanceledException cex)
                        {
                            Trace.WriteLine("Canceled");
                            _events.Publish("Отменено");
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString(), "Error!");
                            _events.Publish("Ошибка парсинга!");
                            return;
                        }
                    }

                    task.ParsedCount++;

                    if (advert != null)
                    {
                        adverts.Add(advert);
                        Trace.WriteLine(advert.ToString(), "Advert");
                    }
                    else
                    {
                        Trace.WriteLine("Advert was skipped", "Warning");
                    }
                }

                _events.Publish("Завершено");

            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation '" + task.Description + "' has been canceled");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка обработки объявлений");
            }
            finally
            {
                Thread.Sleep(5000);
                task.Stop();
                Tasks.Remove(task);
                Trace.WriteLine("Task has been closed", "Info");
            }
        }

        public void StartTask(ParsingTask task)
        {
            task.Start();
        }

        public void PauseTask(ParsingTask task)
        {
            task.Pause();
        }

        public void StopTask(ParsingTask task)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(3000);
                task.Stop();                
                Tasks.Remove(task);
            });
        }

    }

    public class TaskParsingParams
    {
        public string city;
        public int MaxCount;
        public int Delay;
        public ParsePeriod period;
        public ImportSite site;
        public RealEstateType realType;
        public Usedtype subType;
        public AdvertType advertType;
        public bool useProxy;
    }

    public class ParsingTask : RealEstateTask
    {
        private System.Timers.Timer timer;

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


        private TimeSpan _PassBy = TimeSpan.Zero;
        public TimeSpan PassBy
        {
            get { return _PassBy; }
            set
            {
                _PassBy = value;
                NotifyOfPropertyChange(() => PassBy);
            }
        }


        private TimeSpan _Remaining = TimeSpan.Zero;
        public TimeSpan Remaining
        {
            get { return _Remaining; }
            set
            {
                _Remaining = value;
                NotifyOfPropertyChange(() => Remaining);
            }
        }

        public ParsingTask()
            : base()
        {
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += timer_Elapsed;
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            PassBy = PassBy.Add(new TimeSpan(0, 0, 1));
            if (Remaining != TimeSpan.Zero)
                Remaining = Remaining.Add(-new TimeSpan(0, 0, 1));
        }

        public override void Pause()
        {
            IsRunning = false;
            timer.Stop();
            ps.Pause();
        }

        public override void Stop()
        {
            IsCanceled = true;
            timer.Stop();
            timer.Dispose();
            cs.Cancel();
        }

        public override void Start()
        {
            IsRunning = true;
            timer.Start();

            if (Task.IsCanceled)
            {
                return;
            }

            if (ps.IsPauseRequested)
                ps.UnPause();
            else
            {
                Task.RunSynchronously();
            }
        }


    }
}
