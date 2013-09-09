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

namespace RealEstate.Proxies
{
    [Export(typeof(ProxyManager))]
    public class ProxyManager
    {
        private Object thisLock = new Object();

        public List<IProxySourceReader> Readers = new List<IProxySourceReader>();
        private FileStorage storage = new FileStorage();

        public ProxyManager()
        {
            Readers.Add(new FreeproxySourceReader());
            Readers.Add(new CheckerproxySourceReader());
        }

        public BindableCollection<WebProxy> Proxies = new BindableCollection<WebProxy>();
        public BindableCollection<WebProxy> SuspectedProxies = new BindableCollection<WebProxy>();
        public BindableCollection<WebProxy> RejectedProxies = new BindableCollection<WebProxy>();

        public void RejectProxy(WebProxy proxy)
        {
            lock (thisLock)
            {
                if (this.SuspectedProxies.Contains(proxy))
                    RejectProxyFull(proxy);
                else
                    SuspectedProxies.Add(proxy);
            }         
        }

        public void RejectProxyFull(WebProxy proxy)
        {
            lock (thisLock)
            {
                this.Proxies.Remove(proxy);
                this.SuspectedProxies.Remove(proxy);
                this.RejectedProxies.Add(proxy); 
            }
        }

        public void Clear()
        {
            Proxies.Clear();
            RejectedProxies.Clear();
            SuspectedProxies.Clear();
        }

        public void Restore()
        {
            Proxies.Clear();
            var proxies = storage.LoadFromFile();
            if(proxies != null)
                Proxies.AddRange(proxies);
        }

        public void Save()
        {
            storage.SaveToFile(Proxies);
        }
    }
}
