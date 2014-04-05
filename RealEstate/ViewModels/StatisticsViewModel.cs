using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Parsing;
using RealEstate.Parsing.Parsers;
using RealEstate.Proxies;
using RealEstate.Settings;
using RealEstate.Statistics;
using RealEstate.TaskManagers;
using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.ViewModels
{
    [Export(typeof(StatisticsViewModel))]
    public class StatisticsViewModel : ValidatingScreen<AdvertViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;
        private readonly CityManager _cityManager;
        private readonly ImportManager _importManager;
        private readonly ParsingManager _parsingManager;
        private readonly StatisticsManager _statManager;
        private readonly CityParser _cityParser;

        [ImportingConstructor]
        public StatisticsViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager,
            CityManager cityManager, ImportManager importManager, ParsingManager parsingManager, StatisticsManager stat,
            CityParser cityParser)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _importManager = importManager;
            _parsingManager = parsingManager;
            _statManager = stat;
            _cityParser = cityParser;
            events.Subscribe(this);
            this.DisplayName = "Статистика";

            NewType = new StatisticTabViewModel(_cityManager, _events);
            UsedType = new StatisticTabViewModel(_cityManager, _events);
            PassType = new StatisticTabViewModel(_cityManager, _events);
        }

        StatisticTabViewModel NewType;
        StatisticTabViewModel UsedType;
        StatisticTabViewModel PassType;

        protected override void OnInitialize()
        {
            NewType.DisplayName = "Новостройки";
            UsedType.DisplayName = "Вторичное";
            PassType.DisplayName = "Аренда";

            StatItems.Add(NewType);
            StatItems.Add(UsedType);
            StatItems.Add(PassType);
        }

        private ObservableCollection<IScreen> _StatItems = new ObservableCollection<IScreen>();
        public ObservableCollection<IScreen> StatItems
        {
            get { return _StatItems; }
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

        private int _Delay = 15;
        [Range(0, 100)]
        public int Delay
        {
            get { return _Delay; }
            set
            {
                _Delay = value;
                NotifyOfPropertyChange(() => Delay);
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
                if (this.ImportSite == Parsing.ImportSite.All)
                {
                    foreach (var site in Enum.GetValues(typeof(ImportSite)))
                    {
                        var s = (ImportSite)site;

                        if (s != Parsing.ImportSite.All)
                        {
                            ParsingTask realTask = new ParsingTask();
                            realTask.Description = _importManager.GetSiteName(s);
                            realTask.Task = new Task(() => StartInternal(ImportSite, UseProxy, Delay, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                            Tasks.Add(realTask);
                            _taskManager.AddTask(realTask);
                        }
                    }
                }
                else
                {
                    ParsingTask realTask = new ParsingTask();
                    realTask.Description = _importManager.GetSiteName(ImportSite);
                    realTask.Task = new Task(() => StartInternal(ImportSite, UseProxy, Delay, realTask.cs.Token, realTask.ps.PauseToken, realTask));
                    Tasks.Add(realTask);
                    _taskManager.AddTask(realTask);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка запуска статистики!");
            }

            NotifyOfPropertyChange(() => ShowTasks);
        }

        public void StartInternal(ImportSite site, bool useProxy, int delay, CancellationToken ct, PauseToken pt, ParsingTask task)
        {
            try
            {
                if (ct.IsCancellationRequested)
                { _events.Publish("Отменено"); return; }
                if (pt.IsPauseRequested)
                    pt.WaitUntillPaused();

                var statItems = new List<StatisticItem>();
                foreach (var city in _cityManager.NotSelectedCities)
                {
                    if (city.City == CityWrap.ALL) continue;

                    var c = "";
                    if (site == Parsing.ImportSite.Avito)
                        c = city.AvitoKey;
                    else if (site == Parsing.ImportSite.Hands)
                        c = city.HandsKey;

                    var url1 = _statManager.BuildUrl(site, c, RealEstateType.Apartments, AdvertType.Sell, Usedtype.New);
                    statItems.Add(new StatisticItem()
                    {
                        Url = url1,
                        aType = AdvertType.Sell,
                        City = city.City,
                        CityKey = c,
                        rType = RealEstateType.Apartments,
                        Site = site,
                        uType = Usedtype.New
                    });

                    var url2 = _statManager.BuildUrl(site, c, RealEstateType.Apartments, AdvertType.Sell, Usedtype.Used);
                    statItems.Add(new StatisticItem()
                    {
                        Url = url2,
                        aType = AdvertType.Sell,
                        City = city.City,
                        CityKey = c,
                        rType = RealEstateType.Apartments,
                        Site = site,
                        uType = Usedtype.Used
                    });

                    var url3 = _statManager.BuildUrl(site, c, RealEstateType.Apartments, AdvertType.Pass, Usedtype.All);
                    statItems.Add(new StatisticItem()
                    {
                        Url = url3,
                        aType = AdvertType.Pass,
                        City = city.City,
                        CityKey = c,
                        rType = RealEstateType.Apartments,
                        Site = site,
                        uType = Usedtype.All
                    });
                }

                App.Current.Dispatcher.Invoke((System.Action)(() =>
                    {
                        foreach (var url in statItems)
                        {
                            task.SourceUrls.Add(url.Url);
                        }
                    }));

                ParserBase parser = ParsersFactory.GetParser(site);
                int maxattempt = SettingsStore.MaxParsingAttemptCount;
                task.TotalCount = statItems.Count;
                List<long> spans = new List<long>();


                foreach (var item in statItems)
                {
                    DateTime start = DateTime.Now;
                    try
                    {
                        var count = parser.GetTotalCount(item.Url, _proxyManager, useProxy, ct);

                        pt.WaitUntillPaused();

                        App.Current.Dispatcher.Invoke((System.Action)(() =>
                        {
                            StatisticTabViewModel model = null;
                            if (item.aType == AdvertType.Pass)
                            {
                                model = PassType;
                            }
                            else if (item.uType == Usedtype.New && item.aType == AdvertType.Sell)
                            {
                                model = NewType;
                            }
                            else if (item.uType == Usedtype.Used && item.aType == AdvertType.Sell)
                            {
                                model = UsedType;
                            }

                            if (model != null)
                            {
                                var viewItem = model.Items.SingleOrDefault(i => i.City == item.City);
                                if (viewItem == null)
                                {
                                    viewItem = new StatViewItem() { City = item.City };
                                    model.Items.Add(viewItem);
                                }

                                if (item.Site == Parsing.ImportSite.Avito)
                                    viewItem.AvitoCount = count;
                                else if (item.Site == Parsing.ImportSite.Hands)
                                    viewItem.HandsCount = count;
                            }
                        }));


                        Thread.Sleep(Delay * 1000);
                    }
                    catch (OperationCanceledException)
                    {
                        task.Remaining = new TimeSpan();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Stat error!");
                    }

                    task.PerformStep(DateTime.Now - start);
                }

                _events.Publish("Завершено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка парсинга статистики!");
            }
            finally
            {
                task.Stop();
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
                task.Stop();
                Thread.Sleep(3000);
                Tasks.Remove(task);
                NotifyOfPropertyChange(() => ShowTasks);
            });
        }

        public void RemoveTask(ParsingTask task)
        {
            Tasks.Remove(task);
            NotifyOfPropertyChange(() => ShowTasks);
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        public bool ShowTasks
        {
            get { return Tasks.Count != 0; }
        }

        const string UsedFileName = "UsedLastReport.report";
        const string NewFileName = "NewLastReport.report";
        const string PassFileName = "PassLastReport.report";
        public void Save()
        {
            try
            {
                _statManager.Save(UsedType.Items.ToList(), UsedFileName);
                _statManager.Save(NewType.Items.ToList(), NewFileName);
                _statManager.Save(PassType.Items.ToList(), PassFileName);
                _events.Publish("Сохранено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Saving error");
                _events.Publish("Ошибка сохранения");
            }
        }

        public void RestoreLast()
        {
            try
            {
                var list = _statManager.Restore(UsedFileName);


                UsedType.Items.Clear();
                NewType.Items.Clear();
                PassType.Items.Clear();

                foreach (var item in list)
                    UsedType.Items.Add(item);
                list = _statManager.Restore(NewFileName);
                foreach (var item in list)
                    NewType.Items.Add(item);
                list = _statManager.Restore(PassFileName);
                foreach (var item in list)
                    PassType.Items.Add(item);

                _events.Publish("Воссстановлено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Restore error");
                _events.Publish("Ошибка восстановления");
            }
        }
    }
}
