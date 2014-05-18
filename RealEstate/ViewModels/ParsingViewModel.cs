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
using System.IO;
using System.ComponentModel.DataAnnotations;

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
        private readonly AdvertsManager _advertsManager;
        private readonly ImagesManager _imagesManager;
        private readonly SmartProcessor _smartProcessor;
        private readonly ExportingManager _exportingManager;

        private System.Timers.Timer autoTimer = new System.Timers.Timer();

        [ImportingConstructor]
        public ParsingViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager,
            CityManager cityManager, ImportManager importManager, ParserSettingManager parserSettingManager,
            AdvertsManager advertsManager, ImagesManager imagesManager,
            SmartProcessor smartProcessor, ExportingManager exportingManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            _parserSettingManager = parserSettingManager;
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

            ImportSite = Parsing.ImportSite.Avito; //if change, do on view too
            Usedtype = Parsing.Usedtype.All;

            autoTimer.Interval = 1000 * 60 * 1;
            autoTimer.Elapsed += autoTimer_Elapsed;
            autoTimer.AutoReset = true;
            autoTimer.Start();
        }

        private void autoTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (AutoStart && !Tasks.Any(t => t.IsRunning) && AutoStartValue == DateTime.Now.Hour)
            {
                _events.Publish("Автостарт парсинга по таймеру...");
                Start(true);
            }
            else if (AutoStart && DateTime.Now.Hour > AutoStopValue)
            {
                Stop();
                _events.Publish("Автостоп парсинга.");
            }
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
            if (city.City == "Все")
            {
                bool isActive = Cities.First(c => c.City == "Все").IsActive;
                foreach (var cit in Cities)
                {
                    cit.IsActive = isActive;
                }
            }

            NotifyOfPropertyChange(() => TotalCheckedCities);
        }

        public int TotalCheckedCities
        {
            get { return Cities.Count(c => c.IsActive); }
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

        private bool _PhoneImport = false;
        public bool PhoneImport
        {
            get { return _PhoneImport; }
            set
            {
                _PhoneImport = value;
                NotifyOfPropertyChange(() => PhoneImport);

                if (PhoneImport)
                {
                    AutoExport = false;
                    UseProxy = true;
                }
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

        private bool _AutoStart = false;
        public bool AutoStart
        {
            get { return _AutoStart; }
            set
            {
                _AutoStart = value;
                NotifyOfPropertyChange(() => AutoStart);
            }
        }

        private int _AutoStartValue = 0;
        [Range(1, 2400)]
        public int AutoStartValue
        {
            get { return _AutoStartValue; }
            set
            {
                _AutoStartValue = value;
                NotifyOfPropertyChange(() => AutoStartValue);
            }
        }

        private int _AutoStopValue = 6;
        [Range(1, 2400)]
        public int AutoStopValue
        {
            get { return _AutoStopValue; }
            set
            {
                _AutoStopValue = value;
                NotifyOfPropertyChange(() => AutoStopValue);
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

        public void Start(bool bytimer = false)
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
                            param.phoneImport = PhoneImport;

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
                    param.phoneImport = PhoneImport;

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
                    param.site, param.realType, param.subType);

                if (!settings.Any(s => s.Urls.Any()))
                {
                    task.Stop();
                    App.Current.Dispatcher.Invoke((System.Action)(() =>
                       {
                           Tasks.Remove(task);
                       }));
                    return;
                }

                var urls = new List<string>();
                var exportSitesId = new List<KeyValuePair<string, int>>();

                foreach (var setting in settings)
                {
                    if (setting.Urls != null)
                    {
                        foreach (var Url in setting.Urls)
                        {
                            var url = Url.Url;
                            if (!urls.Contains(url))
                            {
                                urls.Add(url);
                            }

                            exportSitesId.Add(new KeyValuePair<string, int>(url, Url.ParserSetting.ExportSite.Id));
                        }
                    }
                }

                App.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        foreach (var item in urls)
                        {
                            task.SourceUrls.Add(item);
                        }
                    }));

                int maxattempt = SettingsStore.MaxParsingAttemptCount;

                WebProxy proxy = param.useProxy ? _proxyManager.GetNextProxy() : null;

                List<AdvertHeader> headers = new List<AdvertHeader>();
                ParserBase parser = ParsersFactory.GetParser(param.site);

                urls.AsParallel().WithDegreeOfParallelism(SettingsStore.ThreadsCount).WithCancellation(ct).ForAll((url) =>
                {
                    if (pt.IsPauseRequested)
                        pt.WaitUntillPaused();

                    ct.ThrowIfCancellationRequested();

                    var hds = parser.LoadHeaders(url, ParserSetting.GetDate(param.period), param, maxattempt * 2, _proxyManager, ct);

                    headers.AddRange(hds);
                });

                task.TotalCount = headers.Count;

                List<Advert> adverts = new List<Advert>();

                headers.AsParallel()
                    .WithCancellation(ct).WithDegreeOfParallelism(SettingsStore.ThreadsCount < 0 ? 1 : SettingsStore.ThreadsCount)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .ForAll((header) =>
                //foreach(var header in headers)
                {
                    ParseHeaderInternal(param, ref ct, pt, task, exportSitesId, maxattempt, proxy, header, parser);
                }
                );

                task.Progress = 100;
                _events.Publish("Завершено");

            }
            catch (AggregateException aex)
            {
                if (aex.InnerExceptions[0] is OperationCanceledException)
                {
                    Trace.WriteLine("Operation '" + task.Description + "' has been canceled");
                    _events.Publish("Парсинг отменён");
                }
                else
                {
                    Trace.WriteLine(aex.InnerException, "Error");
                }
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation '" + task.Description + "' has been canceled");
                _events.Publish("Парсинг отменён");
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

        private void ParseHeaderInternal(TaskParsingParams param, ref CancellationToken ct, PauseToken pt, ParsingTask task, List<KeyValuePair<string, int>> exportSitesId, int maxattempt, WebProxy proxy, AdvertHeader header, ParserBase parser)
        {

            bool parsed = !param.phoneImport && _advertsManager.IsParsed(header.Url);

            DateTime start = DateTime.Now;
            var attempt = 0;
            Advert advert = null;

            if (!parsed)
            {
                while (attempt < maxattempt)
                {
                    attempt++;
                    ct.ThrowIfCancellationRequested();
                    if (pt.IsPauseRequested)
                        pt.WaitUntillPaused();

                    Thread.Sleep(param.Delay * 1000);

                    proxy = param.useProxy ? _proxyManager.GetNextProxy() : null;
                    try
                    {
                        advert = parser.Parse(header, proxy, ct, pt, param.phoneImport);
                        _proxyManager.SuccessProxy(proxy);
                        break;
                    }
                    catch (TimeoutException tix)
                    {
                        Trace.WriteLine(tix.Message, "Web error");
                        _proxyManager.RejectProxy(proxy);
                    }
                    catch (InvalidDataException iex)
                    {
                        Trace.WriteLine(iex.Message, "IO error");
                        _proxyManager.RejectProxyFull(proxy);
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
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString(), "Error!");
                        _events.Publish("Ошибка парсинга!");
                    }
                }
            }
            else
            {
                int failed = 0;
                while (failed < 5)
                {
                    try
                    {
                        failed++;
                        advert = _advertsManager.GetParsed(header.Url);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Get parsed");
                        Thread.Sleep(200);
                    }
                }
            }

            try
            {
                if (advert != null)
                {
                    if (!parsed || param.phoneImport)
                    {
                        advert.ParsingNumber = _advertsManager.LastParsingNumber;
                    }

                    var advertIsOk = param.phoneImport;
                    if (!parsed && !param.phoneImport)
                        advertIsOk = _smartProcessor.Process(advert, param);

                    if (advertIsOk || parsed)
                    {
                        if (SettingsStore.LogSuccessAdverts)
                            Trace.WriteLine(advert, "Advert");

                        var ids = exportSitesId.Where(e => e.Key == header.SourceUrl).Select(e => e.Value).ToArray();

                        int failed = 0;
                        while (failed < 5)
                        {
                            try
                            {
                                failed++;
                                _advertsManager.Save(advert, ids, param.phoneImport);
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (failed == 5)
                                    throw;
                                Trace.WriteLine(ex.Message, "Saving adverts error");
                                Thread.Sleep(200);
                            }
                        }

                        bool checkUniq = false;
                        int failedUniq = 0;

                        while (failedUniq < 5)
                        {
                            try
                            {
                                failedUniq++;
                                checkUniq = CheckUniq(advert, param.uniq);
                                break;
                            }
                            catch (Exception ex)
                            {
                                if (failedUniq == 5)
                                    throw;
                                else
                                {
                                    Trace.WriteLine(ex);
                                    Thread.Sleep(200);
                                }
                            }
                        }

                        if (!param.phoneImport && checkUniq)
                        {

                            int failedImg = 0;
                            while (failedImg < 5)
                            {
                                try
                                {
                                    failedImg++;
                                    if (SettingsStore.SaveImages)
                                        _imagesManager.DownloadImages(advert.Images, ct, advert.ImportSite);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(ex.Message, "DownloadImages");
                                    Thread.Sleep(200);
                                }
                            }

                            if (param.autoExport)
                            {
                                var cov = _smartProcessor.ComputeCoverage(advert);
                                if (cov > 0.6)
                                    if (!param.onlyImage || (param.onlyImage && advert.ContainsImages))
                                    {
                                        int failedInsert = 0;
                                        while (failedInsert < 5)
                                        {
                                            failedInsert++;
                                            try
                                            {
                                                lock (_exportingManager)
                                                {
                                                    _exportingManager.AddAdvertToExport(advert.Id);
                                                }
                                                break;
                                            }
                                            catch (Exception ex)
                                            {
                                                if (failedInsert == 5)
                                                    throw;
                                                else
                                                {
                                                    Trace.WriteLine(ex.Message);
                                                    Thread.Sleep(200);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Trace.WriteLine("Advert (" + advert.Id + ") skipped due the lack of pictures");
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
                throw;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error:" + ex.ToString());
            }

            task.PerformStep(DateTime.Now - start);
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
        public bool phoneImport;
        public bool useProxy;
        public bool autoExport;
        public bool onlyImage;
        public UniqueEnum uniq;
    }

    public class ParsingTask : RealEstateTask
    {
        private System.Timers.Timer timer;
        List<long> spans = new List<long>();

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

        internal void PerformStep(TimeSpan span)
        {
            ParsedCount++;

            if (TotalCount > 0)
            {
                Progress = ((double)ParsedCount / (double)TotalCount) * 100;

                spans.Add(span.Ticks);

                Remaining = new TimeSpan(Convert.ToInt64(spans.Skip(Math.Max(0, spans.Count - 100)).Average()) * (TotalCount - ParsedCount));
            }
        }
    }
}
