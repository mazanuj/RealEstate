using RealEstate.Parsing;

namespace RealEstate.SmartProcessing
{
    public static class AddressDataBase
    {
        public static void ProcessAddress(Advert advert)
        {
            switch (advert.City)
            {
                case "Лесная Поляна":
                    advert.Distinct = "Заволжский";
                    break;
            }
        }
    }
}
