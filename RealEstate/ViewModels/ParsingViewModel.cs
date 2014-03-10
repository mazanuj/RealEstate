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
using RealEstate.SmartProcessing;
using System.Collections.ObjectModel;
using RealEstate.Exporting;

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
        private readonly AdvertsManager _advertsManager;
        private readonly ImagesManager _imagesManager;
        private readonly SmartProcessor _smartProcessor;
        private readonly ExportingManager _exportingManager;
        private const int MAX_COUNT_PARSED = 100;

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager,
            CityManager cityManager, ImportManager importManager, ParserSettingManager parserSettingManager,
            ParsingManager parsingManager, AdvertsManager advertsManager, ImagesManager imagesManager,
            SmartProcessor smartProcessor, ExportingManager exportingManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            _parserSettingManager = parserSettingManager;
            _parsingManager = parsingManager;
            _advertsManager = advertsManager;
            _imagesManager = imagesManager;
            _smartProcessor = smartProcessor;
            _exportingManager = exportingManager;
            events.Subscribe(this);
            DisplayName = "Главная";
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            ImportSite = Parsing.ImportSite.All; //if change, do on view too
            Usedtype = Parsing.Usedtype.All;
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

        public void CityChecked(CityWrap city)
        {
            if(city.City == "Все")
            {
                bool isActive = Cities.First(c => c.City == "Все").IsActive;
                foreach (var cit in Cities)
                {
                    cit.IsActive = isActive;
                }
            }
        }

        private bool _AutoExport = true;
        public bool AutoExport
        {
            get { return _AutoExport; }
            set
            {
                _AutoExport = value;
                NotifyOfPropertyChange(() => AutoExport);
            }
        }


        private ParsePeriod _ParsePeriod = ParsePeriod.All;
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


        private bool _UseProxy = false;
        public bool UseProxy
        {
            get { return _UseProxy; }
            set
            {
                _UseProxy = value;
                NotifyOfPropertyChange(() => UseProxy);
            }
        }

        private bool _OnlyImage = true;
        public bool OnlyImage
        {
            get { return _OnlyImage; }
            set
            {
                _OnlyImage = value;
                NotifyOfPropertyChange(() => OnlyImage);
            }
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

        public BindableCollection<ParsingTask> _tasks = new BindableCollection<ParsingTask>();
        public BindableCollection<ParsingTask> Tasks
        {
            get
            {
                return _tasks;
            }
        }

        public void Stop()
        {
            foreach (var task in Tasks)
            {
                task.Stop();
            }
        }

        public void Pause()
        {
            foreach (var task in Tasks)
            {
                task.Pause();
            }
        }

        public void Start()
        {
            try
            {
                _advertsManager.IncrementParsingNumber();

                if (this.ImportSite == Parsing.ImportSite.All)
                {
                    foreach (var site in Enum.GetValues(typeof(ImportSite)))
                    {
                        var s = (ImportSite)site;

                        if (s != Parsing.ImportSite.All)
                        {
                            TaskParsingParams param = new TaskParsingParams();

                            param.cities = Cities.Where(c => c.IsActive).Select(c => c.City);
                            param.period = this.ParsePeriod;
                            param.site = s;
                            param.realType = this.RealEstateType;
                            param.subType = this.Usedtype;
                            param.advertType = this.AdvertType;
                            param.useProxy = UseProxy;
                            param.autoExport = AutoExport;
                            param.Delay = ImportSites.First(i => i.Site == s).Delay;
                            param.MaxCount = ImportSites.First(i => i.Site == s).Deep;
                            param.onlyImage = OnlyImage;
                            param.uniq = Unique;

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

                    param.cities = Cities.Where(c => c.IsActive).Select(s => s.City);
                    param.period = this.ParsePeriod;
                    param.site = this.ImportSite;
                    param.realType = this.RealEstateType;
                    param.subType = this.Usedtype;
                    param.advertType = this.AdvertType;
                    param.useProxy = UseProxy;
                    param.autoExport = AutoExport;
                    param.onlyImage = OnlyImage;
                    param.Delay = ImportSites.First(i => i.Site == this.ImportSite).Delay;
                    param.MaxCount = ImportSites.First(i => i.Site == this.ImportSite).Deep;
                    param.uniq = Unique;

                    ParsingTask realTask = new ParsingTask();
                    realTask.Description = _importManager.GetSiteName(param.site);
                    realTask.Task = new Task(() => StartInternal(param, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                    Tasks.Add(realTask);
                    _taskManager.AddTask(realTask);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка запуска парсинга!");
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

                var settings = _parserSettingManager.FindSettings(param.advertType, param.cities,
                    param.site, param.period, param.realType, param.subType);


                bool any = false;
                foreach (var setting in settings)
                {
                    if (setting.Urls != null)
                        foreach (var url in setting.Urls.Select(u => u.Url))
                        {
                            any = true;
                        }
                }

                if (!any)
                {
                    task.Stop();
                    App.Current.Dispatcher.Invoke((System.Action)(() =>
                   {
                       Tasks.Remove(task);
                   }));
                    return;
                }

                App.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        foreach (var setting in settings)
                        {
                            if (setting.Urls != null)
                                foreach (var url in setting.Urls.Select(u => u.Url))
                                {
                                    task.SourceUrls.Add(url);
                                }
                        }
                    }));

                int maxattempt = SettingsStore.MaxParsingAttemptCount;

                WebProxy proxy = param.useProxy ? _proxyManager.GetNextProxy() : null;
                var headers = _parsingManager.LoadHeaders(param, settings, ct, pt, maxattempt, _proxyManager);

                task.TotalCount = headers.Count;

                List<Advert> adverts = new List<Advert>();
                List<long> spans = new List<long>();
                ParserBase parser = ParsersFactory.GetParser(param.site);

                int attempt = 0;
                Advert last = null;
                Advert advert = null;
                bool parsed = false;
                int prsCount = 0;

                for (int i = 0; i < headers.Count; i++)
                {
                    parsed = _advertsManager.IsParsed(headers[i].Url);

                    DateTime start = DateTime.Now;

                    if (!parsed)
                    {
                        prsCount = 0;
                        last = advert;
                        advert = null;

                        attempt = 0;
                        int blocked = 0;

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
                                Trace.WriteLine(wex.Message, "Web error");
                                _proxyManager.RejectProxy(proxy);

                                if ((HttpWebResponse)wex.Response != null)
                                {
                                    if (((HttpWebResponse)wex.Response).StatusCode == HttpStatusCode.Forbidden)
                                    {
                                        _proxyManager.RejectProxyFull(proxy);

                                        blocked++;
                                        if (blocked > maxattempt - 2)
                                        {
                                            _events.Publish("Сервер заблокировал доступ. Операция приостановлена");
                                            return;
                                        }
                                    }
                                }

                            }
                            catch (System.IO.IOException)
                            {
                                Trace.WriteLine("IO error");
                                _proxyManager.RejectProxy(proxy);
                            }
                            catch (BadResponseException)
                            {
                                Trace.WriteLine("Bad response from proxy");
                                _proxyManager.RejectProxy(proxy);
                            }
                            catch (ParsingException pex)
                            {
                                Trace.WriteLine(pex.Message + ": " + pex.UnrecognizedData, "Unrecognized data");
                                if (attempt + 1 >= maxattempt)
                                    break;
                                else
                                    attempt = maxattempt - 2;
                            }
                            catch (OperationCanceledException)
                            {
                                Trace.WriteLine("Canceled");
                                _events.Publish("Отменено");
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex.ToString(), "Error!");
                                _events.Publish("Ошибка парсинга!");
                            }
                        }
                    }

                    if (parsed)
                    {
                        if (prsCount > MAX_COUNT_PARSED)
                        {
                            _events.Publish("Задание завершено. Текущий url уже был обработан");
                            Trace.WriteLine("Task was stopped. Current url already parsed");
                            break;
                        }

                        int failed = 0;
                        while (advert != null && failed < 5)
                        {
                            try
                            {
                                failed++;
                                advert = _advertsManager.GetParsed(headers[i].Url);
                            }
                            catch (Exception)
                            {
                                Thread.Sleep(200);
                            } 
                        }
                        
                        prsCount++;
                    }

                    task.ParsedCount++;

                    try
                    {
                        if (advert != null)
                        {
                            if (!parsed)
                            {
                                advert.ParsingNumber = _advertsManager.LastParsingNumber;
                                if (advert.ImportSite == Parsing.ImportSite.Hands)
                                {
                                    if (last != null && advert != null)
                                    {
                                        if (!String.IsNullOrEmpty(last.Address) && !String.IsNullOrEmpty(advert.Address))
                                        {
                                            if (last.Street == advert.Street && last.Rooms == advert.Rooms && last.House == advert.House)
                                            {
                                                Trace.WriteLine("Skiped by last has the same address");
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }

                            if (_smartProcessor.Process(advert, param))
                            {
                                if (advert.ImportSite == Parsing.ImportSite.Hands)
                                {
                                    if (last != null && advert != null)
                                    {
                                        if (!String.IsNullOrEmpty(last.Address) && !String.IsNullOrEmpty(advert.Address))
                                        {
                                            if (last.Street == advert.Street && last.Rooms == advert.Rooms && last.House == advert.House)
                                            {
                                                Trace.WriteLine("Skiped by last has the same address");
                                                continue;
                                            }
                                        }
                                    }
                                }

                                if (SettingsStore.LogSuccessAdverts)
                                    Trace.WriteLine(advert.ToString(), "Advert");

                                _advertsManager.Save(advert, headers[i].Setting);

                                if (CheckUniq(advert, param.uniq))
                                {

                                    if (SettingsStore.SaveImages)
                                        _imagesManager.DownloadImages(advert.Images, ct, advert.ImportSite);

                                    if (param.autoExport && (SettingsStore.ExportParsed || (!SettingsStore.ExportParsed && !parsed)))
                                    {
                                        var cov = _smartProcessor.ComputeCoverage(advert);
                                        if (cov > 0.6)
                                            if (!param.onlyImage || (param.onlyImage && advert.ContainsImages))
                                                _exportingManager.AddAdvertToExport(advert);
                                            else
                                            {
                                                Trace.WriteLine("Advert ("+ advert.Id +") skipped due the lack of pictures");
                                            }
                                        else
                                            Trace.WriteLine("Advert skipped as empty. Coverage = " + cov.ToString("P0"), "Skipped by smart processor");
                                    }
                                }
                            }
                            else
                            {
                                //Trace.TraceInformation("Advert was skipped due smart processor rule");
                            }
                        }
                        else
                        {
                            Trace.WriteLine("Advert was skipped", "Warning");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Trace.WriteLine("Operation '" + task.Description + "' has been canceled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Ошибка:" + ex.Message);
                    }


                    if (task.TotalCount > 0)
                    {
                        task.Progress = ((double)task.ParsedCount / (double)task.TotalCount) * 100;

                        DateTime finish = DateTime.Now;
                        spans.Add((finish - start).Ticks);

                        task.Remaining = new TimeSpan(Convert.ToInt64(spans.Average()) * (task.TotalCount - task.ParsedCount));
                    }

                }

                task.Progress = 100;
                _events.Publish("Завершено");

            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation '" + task.Description + "' has been canceled");
            }
            catch (ParsingException pex)
            {
                Trace.WriteLine(pex.Message, "Error!");
                _events.Publish("Ошибка загрузки объявлений");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка обработки объявлений");
            }
            finally
            {
                task.Stop();
                Trace.WriteLine("Task has been closed", "Info");
            }
        }

        private bool CheckUniq(Advert advert, UniqueEnum uniqueEnum)
        {
            switch (uniqueEnum)
            {
                case UniqueEnum.All:
                    return true;
                case UniqueEnum.New:
                    return _advertsManager.IsAdvertNew(advert);
                case UniqueEnum.Unique:
                    return _advertsManager.IsAdvertUnique(advert);
                default:
                    throw new NotImplementedException();
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
                task.Stop();
                Thread.Sleep(3000);
                Tasks.Remove(task);
            });
        }

        public void RemoveTask(ParsingTask task)
        {
            Tasks.Remove(task);
        }

    }

    public class TaskParsingParams
    {
        public IEnumerable<string> cities;
        public int MaxCount;
        public int Delay;
        public ParsePeriod period;
        public ImportSite site;
        public RealEstateType realType;
        public Usedtype subType;
        public AdvertType advertType;
        public bool useProxy;
        public bool autoExport;
        public bool onlyImage;
        public UniqueEnum uniq;
    }

    public class ParsingTask : RealEstateTask
    {
        private System.Timers.Timer timer;

        private ObservableCollection<string> _SourceUrls = new ObservableCollection<string>();
        public ObservableCollection<string> SourceUrls
        {
            get { return _SourceUrls; }
        }

        private int _TotlaCount = 0;
        public int TotalCount
        {
            get { return _TotlaCount; }
            set
            {
                _TotlaCount = value;
                NotifyOfPropertyChange(() => TotalCount);
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

        private double _Progress = 0;
        public double Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                NotifyOfPropertyChange(() => Progress);
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
            if (!IsCanceled)
            {
                IsCanceled = true;
                timer.Stop();
                timer.Dispose();
                cs.Cancel();
            }
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
                Task.Start();
            }
        }


    }
}
