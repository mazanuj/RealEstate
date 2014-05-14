using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;
using Caliburn.Micro.Validation;
using System.Threading.Tasks;
using System.Threading;
using RealEstate.TaskManagers;
using KTF.Proxy.Readers;
using RealEstate.Proxies;
using System.Collections.ObjectModel;
using System.Diagnostics;
using KTF.Proxy;
using System.Net;
using System.Timers;

namespace RealEstate.ViewModels
{
    [Export(typeof(ProxiesViewModel))]
    public class ProxiesViewModel : ValidatingScreen<ProxiesViewModel>, IHandle<ToolsOpenEvent>, IHandle<CriticalErrorEvent>
    {
        const int MIN_PROXIES = 4;
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;

        private System.Timers.Timer timer = new System.Timers.Timer();


        [ImportingConstructor]
        public ProxiesViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            events.Subscribe(this);
            DisplayName = "Прокси";

            timer.Interval = 20 * 1000;
            timer.AutoReset = true;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsUpdating && _proxyManager.Proxies.Count < MIN_PROXIES)
            {
                FromNetUpdate = true;
                UpdateInternal(false);
            }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            FromNetUpdate = true;

            _proxyManager.Readers.ForEach(SourceReaders.Add);
            SelectedSourceReader = SourceReaders.First();   
        }

        protected override void OnDeactivate(bool close)
        {
            if (close)
            {
                Trace.WriteLine("Saving proxies..." );
                _proxyManager.Save();
            }
            base.OnDeactivate(close);
        }

        private bool _FromNetUpdate = false;
        public bool FromNetUpdate
        {
            get { return _FromNetUpdate; }
            set
            {
                _FromNetUpdate = value;
                NotifyOfPropertyChange(() => FromNetUpdate);
            }
        }

        public bool FromFileUpdate
        {
            get { return !_FromNetUpdate; }
            set
            {
                _FromNetUpdate = !value;
                NotifyOfPropertyChange(() => FromFileUpdate);
            }
        }

        private BindableCollection<IProxySourceReader> _SourceReaders = new BindableCollection<IProxySourceReader>();
        public BindableCollection<IProxySourceReader> SourceReaders
        {
            get { return _SourceReaders; }
        }

        public BindableCollection<StatProxy> CheckedProxies
        {
            get { return _proxyManager.Proxies; }
        }

        public BindableCollection<StatProxy> RejectedProxies
        {
            get { return _proxyManager.RejectedProxies; }
        }
        
        private IProxySourceReader _SelectedSourceReader = null;
        public IProxySourceReader SelectedSourceReader
        {
            get { return _SelectedSourceReader; }
            set
            {
                _SelectedSourceReader = value;
                NotifyOfPropertyChange(() => SelectedSourceReader);
            }
        }


        private bool _IsUpdating = false;
        public bool IsUpdating
        {
            get { return _IsUpdating; }
            set
            {
                _IsUpdating = value;
                NotifyOfPropertyChange(() => IsUpdating);
                NotifyOfPropertyChange(() => IsNotUpdating);
            }
        }

        public bool IsNotUpdating
        {
            get
            {
                return !IsUpdating;
            }
        }


        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        RealEstateTask realTask = null;

        public void Update()
        {
            UpdateInternal(true);
        }

        private void UpdateInternal(bool clean)
        {
            IsUpdating = true;
            CanCancelUpdate = true;
            realTask = new RealEstateTask();
            Progress = 0;
            NotifyOfPropertyChange(() => CanCheckOut);

            if (clean)
            {
                _proxyManager.Clear();
            }


            if (FromNetUpdate)
            {
                realTask.Task = new Task(() => UpdateFromNet(SelectedSourceReader, realTask.cs.Token));
                _taskManager.AddTask(realTask);
            }
            else if (FromFileUpdate)
            {
                realTask.Task = new Task(UpdateFromFile);
                _taskManager.AddTask(realTask);
            }
        }

        private void UpdateFromFile()
        {
            try
            {
                Trace.WriteLine("Selected updating from file");
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.DefaultExt = ".txt";
                dlg.Filter = "Text documents (.txt)|*.txt";
                if (dlg.ShowDialog().Value)
                {
                    string filename = dlg.FileName;

                    Trace.WriteLine("Selected file: " + filename);

                    var storage = new KTF.Proxy.Storage.FileStorage() { FilePath = filename };

                    Trace.WriteLine("Loading proxy from file...");
                    _events.Publish("Загрузка прокси из файла ...");

                    var proxies = storage.LoadFromFile();

                    _proxyManager.Load(proxies, true);

                    Trace.WriteLine("Proxies proxies. Total count: " + proxies.Count());

                    _events.Publish("Прокси загружены");

                    IsUpdating = false;

                }
                else
                {
                    Trace.WriteLine("File not selected");
                    IsUpdating = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error:");
                _events.Publish("Ошибка");
                IsUpdating = false;
            }
        }

        private void UpdateFromNet(IProxySourceReader reader, CancellationToken token)
        {
            try
            {
                if (reader == null)
                {
                    Trace.Write("ParsingSource reader doesn't selected");
                    IsUpdating = false;
                    return;
                }

                Trace.WriteLine("Loading proxy from '" + reader.Name + "' ...");
                _events.Publish("Загрузка прокси из '" + reader.Name + "'...");

                var loaded = reader.GetProxies("", ConnectionType.Any, "", token);
                var loadedCount = loaded.Count();
                total = loadedCount;
                
                Trace.WriteLine("Proxies proxies. Total count: " + loadedCount);

                CheckProxies(token, loaded);

                _events.Publish("Прокси обновлены");
                Progress = 100;

            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation canceled");
                _events.Publish("Отменено");
                CanCancelUpdate = true;
                Progress = 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error:");
                _events.Publish("Ошибка");
                Progress = 0;
            }

            IsUpdating = false;

        }

        private void CheckProxies(CancellationToken token, IEnumerable<WebProxy> proxies)
        {
            _events.Publish("Проверка прокси...");

            check = 0;
            var proxyChecker = new ProxyChecker(SettingsStore.UrlForChecking);
            proxyChecker.Checked += proxyChecker_Checked;
            var @checked = proxyChecker.GetTestedProxies(proxies, token);
            var checkedCount = @checked.Count();

            Trace.WriteLine("Proxies checked. Total count of working proxies: " + checkedCount);
        }

        void proxyChecker_Checked(object sender, WebProxyEventArgs e)
        {
            try
            {
                if (!CanCancelUpdate) return;

                NotifyOfPropertyChange(() => CanCheckOut);

                if (e.IsSuccess)
                {
                    _proxyManager.Add(e.WebProxy);
                }

                check++;
                if (check % 3 == 0)
                {
                    Progress = ((double)check / total) * 100;
                    _events.Publish(String.Format("Проверка прокси... {0:0.#}%", Progress));
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "proxyChecker_Checked");
            }
        }

        int total = 0;
        int check = 0;
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

        public void CancelUpdate()
        {
            _events.Publish("Отмена...");
            realTask.Stop();
            CanCancelUpdate = false;
        }

        private bool _CanCancelUpdate = true;
        public bool CanCancelUpdate
        {
            get { return _CanCancelUpdate; }
            set
            {
                _CanCancelUpdate = value;
                NotifyOfPropertyChange(() => CanCancelUpdate);
            }
        }

        public void CheckOut()
        {
            IsUpdating = true;
            CanCancelUpdate = true;

            realTask = new RealEstateTask();
            realTask.Task = new Task(() => CheckOutWork(realTask.cs.Token));
            _taskManager.AddTask(realTask);
        }

        public void CheckOutWork(CancellationToken token)
        {
            try
            {
                var proxies = CheckedProxies.Select(p => p.Proxy).ToList();

                _proxyManager.Clear();
                total = proxies.Count;
                CheckProxies(token, proxies);

                _events.Publish("Прокси проверены");
                Progress = 100;

               
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Operation canceled");
                _events.Publish("Отменено");
                CanCancelUpdate = true;
                Progress = 0;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error:");
                _events.Publish("Ошибка");
                Progress = 0;
            }

            IsUpdating = false;
        }

        public bool CanCheckOut
        {
            get { return CheckedProxies.Count != 0; }
        }

        public void UpdateWork(CancellationToken token)
        {
            System.Threading.Thread.Sleep(5000);
            if (token.IsCancellationRequested) return;
            DisplayName += "123";
        }

        public void Handle(CriticalErrorEvent message)
        {
            if(realTask != null)
                realTask.cs.Cancel();
        }
    }
}
