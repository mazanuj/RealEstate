using Caliburn.Micro;
using System;

namespace RealEstate.ViewModels
{
    public class ScreenWithToogleTools : Screen
    {
        private bool isToolsOpen = true;
        public Boolean IsToolsOpen
        {
            get { return isToolsOpen; }
            set
            {
                isToolsOpen = value;
                NotifyOfPropertyChange(() => IsToolsOpen);
            }
        }
    }
}
