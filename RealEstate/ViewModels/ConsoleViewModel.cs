using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace RealEstate.ViewModels
{
    [Export(typeof(ConsoleViewModel))]
    public class ConsoleViewModel : PropertyChangedBase
    {
        public ConsoleViewModel()
        {
            
        }
    }
}
