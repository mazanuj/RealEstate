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

namespace RealEstate.ViewModels
{
    [Export(typeof(ProxiesViewModel))]
    public class ProxiesViewModel : ValidatingScreen<ProxiesViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;

        [ImportingConstructor]
        public ProxiesViewModel(IEventAggregator events, TaskManager taskManager,ProxyManager proxyManager )
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            events.Subscribe(this);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            DisplayName = "Прокси";
            IsEnabled = true;
            IsToolsOpen = true;
            FromNetUpdate = true;

            _proxyManager.readers.ForEach(SourceReaders.Add);
            SelectedSourceReader = SourceReaders.First();
        }


        
        private bool _IsEnabled = false;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set
            {
                _IsEnabled = value;
                NotifyOfPropertyChange(() => IsEnabled);
            }
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

        
        private bool _FromFileUpdate = false;
        public bool FromFileUpdate
        {
            get { return _FromFileUpdate; }
            set
            {
                _FromFileUpdate = value;
                NotifyOfPropertyChange(() => FromFileUpdate);
            }
        }

        
        private int _TotalCount = 10;
        public int TotalCount
        {
            get { return _TotalCount; }
            set
            {
                _TotalCount = value;
                NotifyOfPropertyChange(() => TotalCount);
            }
        }


        private BindableCollection<IProxySourceReader> _SourceReaders = new BindableCollection<IProxySourceReader>();
        public BindableCollection<IProxySourceReader> SourceReaders
        {
            get { return _SourceReaders; }
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

        
        private bool _IsUpdateAllow = true;
        public bool IsUpdateAllow
        {
            get { return _IsUpdateAllow; }
            set
            {
                _IsUpdateAllow = value;
                NotifyOfPropertyChange(() => IsUpdateAllow);
            }
        }
                    
                    
        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        CancellationTokenSource s = new CancellationTokenSource();

        public void Update()
        {
            IsUpdateAllow = false;

            if (FromNetUpdate)
                _taskManager.AddTask(new TaskWithDescription(() => UpdateFromNet(SelectedSourceReader, s.Token)));
            else if (FromFileUpdate)
                 _taskManager.AddTask(new TaskWithDescription(UpdateFromFile));
        }

        private void UpdateFromFile()
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
                var proxies = storage.LoadFromFile();
                Trace.WriteLine("Proxies loaded. Total count: " + proxies.Count());


                IsUpdateAllow = true;

            }
            else
            {
                Trace.WriteLine("File not selected");
                IsUpdateAllow = true;
            }
        }

        private void UpdateFromNet(IProxySourceReader reader ,CancellationToken token)
        {
            if (reader == null)
            {
                Trace.Write("Source reader doesn't selected");
                IsUpdateAllow = true;
                return;
            }

            var result = reader.GetProxies("", ConnectionType.Any, "", token);

            IsUpdateAllow = true;

        }

        public void Update2()
        {
            s.Cancel();
            s = new CancellationTokenSource();
        }

        public void UpdateWork(CancellationToken token)
        {
            System.Threading.Thread.Sleep(5000);
            if (token.IsCancellationRequested) return;
            DisplayName += "123";
        }
    }
}
