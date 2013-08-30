using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KTF.Proxy.Readers;
using KTF.Proxy.Storage;
using KTF.Proxy;
using System.ComponentModel.Composition;
using System.Net;

namespace RealEstate.Proxies
{
    [Export(typeof(ProxyManager))]
    public class ProxyManager
    {
        public List<IProxySourceReader> readers = new List<IProxySourceReader>();
        private FileStorage storage = new FileStorage();

        public ProxyManager()
        {
            readers.Add(new FreeproxySourceReader());
            readers.Add(new CheckerproxySourceReader());
        }

        public List<WebProxy> Proxies = new List<WebProxy>();
    }
}
