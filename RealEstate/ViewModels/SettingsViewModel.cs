using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : PropertyChangedBase
    {
    }
}
