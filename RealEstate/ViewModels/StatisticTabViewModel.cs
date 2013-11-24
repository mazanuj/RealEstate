using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(StatisticTabViewModel))]
    public class StatisticTabViewModel : Screen
    {
        private ObservableCollection<StatViewItem> _Items = new ObservableCollection<StatViewItem>();
        public ObservableCollection<StatViewItem> Items
        {
            get { return _Items; }
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
