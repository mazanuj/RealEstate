using Caliburn.Micro;
using System.Net;

namespace RealEstate.Proxies
{
    public class StatProxy : PropertyChangedBase
    {
        const int MIN = -3;
        const int MAX = 3;

        public StatProxy(WebProxy proxy)
        {
            Proxy = proxy;
        }

        public WebProxy Proxy { get; private set; }

        public bool IsGood
        {
            get { return Failed < MAX; }
        }

        private int _failed;
        public int Failed
        {
            get { return _failed; }
            set
            {
                if (value > MAX)
                    _failed = MAX;
                else if (value < MIN)
                    _failed = MIN;
                else
                    _failed = value;
                
                NotifyOfPropertyChange(() => Failed);
            }
        }

        public void SetFailed()
        {
            _failed = MAX;
        }
    }
}
