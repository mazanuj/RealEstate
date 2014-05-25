using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KTF.Proxy.Readers;
using KTF.Proxy.Storage;
using KTF.Proxy;
using System.ComponentModel.Composition;
using System.Net;
using Caliburn.Micro;
using System.Diagnostics;
using System.Timers;

namespace RealEstate.Proxies
{
    [Export(typeof(ProxyManager))]
    public class ProxyManager
    {
        private Object _lock = new Object();

        public List<IProxySourceReader> Readers = new List<IProxySourceReader>();
        private FileStorage storage = new FileStorage();
        private int index = 0;
        private int maxIndex = 0;

        public ProxyManager()
        {
            Readers.Add(new FreeproxySourceReader());
            Readers.Add(new CheckerproxySourceReader());
            Proxies.CollectionChanged += Proxies_CollectionChanged;
        }

        void Proxies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            maxIndex = Proxies.Count;
            index = new Random().Next(maxIndex);
        }

        public BindableCollection<StatProxy> Proxies = new BindableCollection<StatProxy>();
        public BindableCollection<StatProxy> RejectedProxies = new BindableCollection<StatProxy>();

        public WebProxy GetNextProxy()
        {
            lock (_lock)
            {
                if (maxIndex == 0) return null;
                index++;
                if (index >= maxIndex) index = 0;
                return Proxies[index].Proxy;
            }
        }

        public void SuccessProxy(WebProxy proxy)
        {
            lock (_lock)
            {
                if (proxy != null)
                {
                    var stat = Proxies.FirstOrDefault(p => p.Proxy == proxy);
                    if (stat != null)
                    {
                        stat.Failed--;
                    }
                }
            }
        }

        public void RejectProxy(WebProxy proxy)
        {
            if (proxy != null)
            {
                lock (_lock)
                {
                    var stat = Proxies.FirstOrDefault(p => p.Proxy == proxy);
                    if (stat != null)
                    {
                        stat.Failed++;
                        if (!stat.IsGood)
                        {
                            RejectProxyFull(stat.Proxy);
                        }
                    }
                }
            }
        }

        public void RejectProxyFull(WebProxy proxy)
        {
            if (proxy != null)
            {
                var stat = Proxies.FirstOrDefault(p => p.Proxy == proxy);
                if (stat != null)
                {
                    stat.SetFailed();
                    Proxies.Remove(stat);
                    RejectedProxies.Add(stat);
                }
            }
        }

        public void Clear()
        {
            Proxies.Clear();
            RejectedProxies.Clear();
            maxIndex = 0;
        }

        public void Restore()
        {
            Proxies.Clear();
            var proxies = storage.LoadFromFile();
            Load(proxies);
        }

        public void Load(IEnumerable<WebProxy> proxies, bool clean = false)
        {
            if (clean)
            {
                Proxies.Clear();
            }

            if (proxies != null)
            {
                Proxies.AddRange(proxies.Select(p => new StatProxy(p)));
            }
        }

        public void Add(WebProxy proxy)
        {
            Proxies.Add(new StatProxy(proxy));
        }

        public void Save()
        {
            storage.SaveToFile(Proxies.Select(p => p.Proxy));
        }
    }
}
