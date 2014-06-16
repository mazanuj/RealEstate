using Caliburn.Micro.Validation;
using RealEstate.City;
using System.ComponentModel.Composition;

namespace RealEstate.ViewModels
{
    [Export(typeof(CityWrapViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CityWrapViewModel: ValidatingScreen<CityWrapViewModel>
    {
        public CityWrap City { get; set; }

        protected override void OnActivate()
        {
            DisplayName = City.City;
        }
    }
}
