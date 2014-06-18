using System;
using System.Collections.Generic;
using System.Linq;
using RealEstate.Exporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstate.Parsing
{
    public class AdvertHeader
    {
        public string Url { get; set; }
        public DateTime DateSite { get; set; }
        public string SourceUrl { get; set; }
    }

    public class Advert
    {
        public Advert()
        {
            Usedtype = Usedtype.Used;
        }

        public int Id { get; set; } //+
        public string Url { get; set; } //+

        public string Title { get; set; } //+

        public Int64 Price { get; set; } //+

        public string Name { get; set; }  //+
        public string PhoneNumber { get; set; } //+
        public string Email { get; set; }

        public string City { get; set; } //+
        public string Distinct { get; set; }
        public string Address { get; set; } //+
        public string Street { get; set; }
        public string House { get; set; }
        public string HouseStroenie { get; set; }
        public string HousePart { get; set; }
        public string MetroStation { get; set; } //+
        public string AO { get; set; }

        public string MessageShort { get; set; }
        public string MessageFull { get; set; } //+

        public string Rooms { get; set; } //+

        public float AreaFull { get; set; } //+
        public float AreaLiving { get; set; }
        public float AreaKitchen { get; set; }

        public short Floor { get; set; } //+
        public short FloorTotal { get; set; } //+

        [Column("RealEstateType", TypeName = "int")]
        public int RealEstateTypeValue { get; set; }
        [NotMapped]
        public RealEstateType RealEstateType
        {
            get { return (RealEstateType)RealEstateTypeValue; }
            set { RealEstateTypeValue = (int)value; }
        }

        [Column("Usedtype", TypeName = "int")]
        public int UsedtypeValue { get; set; }
        [NotMapped]
        public Usedtype Usedtype
        {
            get { return (Usedtype)UsedtypeValue; }
            set { UsedtypeValue = (int)value; }
        }

        [Column("AdvertType", TypeName = "int")]
        public int AdvertTypeValue { get; set; }
        [NotMapped]
        public AdvertType AdvertType
        {
            get { return (AdvertType)AdvertTypeValue; }
            set { AdvertTypeValue = (int)value; }
        }

        [Column("ImportSite", TypeName = "int")]
        public int ImportSiteValue { get; set; }
        [NotMapped]
        public ImportSite ImportSite
        {
            get { return (ImportSite)ImportSiteValue; }
            set { ImportSiteValue = (int)value; }
        }

        public bool IsFromDeveloper { get; set; }
        public bool IsFromAgency { get; set; }

        public bool isGold { get; set; }

        public bool isUnique { get; set; }

        public string Tag { get; set; }

        public int ParsingNumber { get; set; }

        public string BuildingYear { get; set; }
        public string BuildingQuartal { get; set; }

        public DateTime DateSite { get; set; } //+
        public DateTime DateUpdate { get; set; } //+

        public bool IsYaroslavl
        {
            get { return City == "Ярославль"; }
        }

        public bool ContainsImages
        {
            get { return Images != null && Images.Count > 0; }
        }

        public virtual ICollection<Image> Images { get; set; } //+
        public virtual ICollection<ExportSite> ExportSites { get; set; }

        public override string ToString()
        {
            return String.Format(
                "Rooms: '{0}', Area: '{1:#.0#}', Floor: '{2}', Floor total: '{3}', Seller: '{4}', "
              + "City: '{5}' , Adress: '{6}', Date: '{7:m}', AdverType: '{8}', UsedType: '{9}',Price: '{10}',"
              + "Phone: '{11}', RealEstateType: '{12}'",
                Rooms, AreaFull, Floor, FloorTotal, Name,
                City, Address, DateSite, AdvertType, Usedtype,
                Price, PhoneNumber, RealEstateType);
        }

        public string MessageFullPreview
        {
            get
            {
                return MessageFull != null ? MessageFull.Replace("\n", " ") + "..." : "";
            }
        }

        public string GetKindOf()
        {
            if (String.IsNullOrEmpty(Rooms))
                return "";

            if (Rooms.Contains('1'))
                return "1";
            if (Rooms.Contains('2'))
                return "2";
            if (Rooms.Contains('3'))
                return "3";
            if (Rooms.Contains('4'))
                return "4";
            if (Rooms.Contains('5'))
                return "5+";
            if (Rooms.Contains("туд"))
                return "99";
            return "5+";
        }

        public int GetAO()
        {
            switch (AO)
            {
                case "Северо-Западный":
                case "СЗАО":
                    return 8;
                case "Зеленоградский":
                case "ЗелАО":
                    return 9;
                case "Западный":
                case "ЗАО":
                    return 7;
                case "Юго-Западный":
                case "ЮЗАО":
                    return 6;
                case "Южный":
                case "ЮАО":
                    return 5;
                case "Юго-Восточный":
                case "ЮВАО":
                    return 4;
                case "Восточный":
                case "ВАО":
                    return 3;
                case "Северо-Восточный":
                case "СВАО":
                    return 2;
                case "Северный":
                case "САО":
                    return 1;
                case "Центральный":
                case "ЦАО":
                    return 0;
                default:
                    return -1;
            }
        }

        public string GetCategory()
        {
            if (IsFromAgency)
                return "9";
            if (IsFromDeveloper)
                return "1";
            return "2";
        }
    }

    public class Image
    {
        public int Id { get; set; }
        public string URl { get; set; }

        [NotMapped]
        public string LocalName
        {
            get
            {
                if (String.IsNullOrEmpty(URl)) return null;

                var parts = URl.Split(new[] { '/' });
                return parts.LastOrDefault();
            }
        }

        public Advert Advert { get; set; }
    }

    public enum RealEstateType
    {
        All,
        Apartments,
        House
    }

    public enum Usedtype
    {
        All,
        New,
        Used
    }

    public enum AdvertType
    {
        All,
        Buy,
        Sell,
        Rent,
        Pass
    }
}
