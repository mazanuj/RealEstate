using Caliburn.Micro;
using RealEstate.Parsing.Parsers;
using RealEstate.Proxies;
using RealEstate.Utils;
using RealEstate.ViewModels;
using System.ComponentModel.Composition;
using System.Threading;

namespace RealEstate.City
{
    [Export(typeof(CityParser))]
    public class CityParser
    {
        private readonly ProxyManager _proxyManager;

        [ImportingConstructor]
        public CityParser(ProxyManager proxyManager)
        {
            _proxyManager = proxyManager;
        }

        public void UpdateCities(CancellationToken ct, PauseToken pt, BindableCollection<CityWrap> fullList, ParsingTask task)
        {
            var parser = new AvitoParser();
            parser.UpdateList(ct, pt, _proxyManager, fullList, task);
        }
    }
}
