using Caliburn.Micro;
using RealEstate.City;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace RealEstate.ViewModels
{
    [Export(typeof(StatisticTabViewModel))]
    public class StatisticTabViewModel : Screen
    {
        private readonly CityManager _cityManager;
        private readonly IEventAggregator _event;
        public StatisticTabViewModel(CityManager city, IEventAggregator events)
        {
            _cityManager = city;
            _event = events;
        }

        private ObservableCollection<StatViewItem> _Items = new ObservableCollection<StatViewItem>();
        public ObservableCollection<StatViewItem> Items
        {
            get { return _Items; }
        }

        public void AddCity(StatViewItem item)
        {
            if (!_cityManager.Cities.Any(c => c.City == item.City))
            {
                var cityWrap = _cityManager.NotSelectedCities.FirstOrDefault(c => c.City == item.City);
                if (cityWrap != null)
                {
                    cityWrap.IsSelected = true;
                    _cityManager.Cities.Add(cityWrap);
                    _event.Publish("Добавлено");
                }
            }
            else
                _event.Publish("Город уже есть в списке");
        }
    }

    public class StatViewItem : PropertyChangedBase
    {
        public string City { get; set; }

        private int _AvitoCount = 0;
        public int AvitoCount
        {
            get { return _AvitoCount; }
            set
            {
                _AvitoCount = value;
                NotifyOfPropertyChange(() => AvitoCount);
            }
        }

        private int _HandsCount = 0;
        public int HandsCount
        {
            get { return _HandsCount; }
            set
            {
                _HandsCount = value;
                NotifyOfPropertyChange(() => HandsCount);
            }
        }
    }
}
