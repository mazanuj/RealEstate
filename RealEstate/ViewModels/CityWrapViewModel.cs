using RealEstate.City;
using System.ComponentModel.Composition;
using RealEstate.Validation;

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
