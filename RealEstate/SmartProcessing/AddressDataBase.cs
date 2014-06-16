using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
