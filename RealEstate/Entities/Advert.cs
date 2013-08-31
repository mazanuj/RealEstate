using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Entities
{
    public class Advert
    {
        public int Id { get; set; }
        public string Url { get; set; }

        public string Title { get; set; }
        public string Name { get; set; }

        public string PhoneNumber { get; set; }
        public string Email { get; set; }

        public string City { get; set; }
        public string Distinct { get; set; }
        public string Address { get; set; }
        public string MetroStation { get; set; }

        public string MessageShort { get; set; }
        public string MessageFull { get; set; }

        public short Rooms { get; set; }

        public float AreaFull { get; set; }
        public float AreaLiving { get; set; }
        public float AreaKitchen { get; set; }

        public short Floor { get; set; }
        public short FloorTotal { get; set; }

        public RealEstateType RealEstateType { get; set; }
        public Usedtype Usedtype { get; set; }
        public AdvertType AdvertType { get; set; }

        public bool isGold { get; set; }

        public bool isUnique { get; set; }

        public string Tag { get; set; }

        public DateTime DateSite{ get; set; }
        public DateTime DateUpdate { get; set; }

        public virtual ICollection<Image> Images { get; set; }

    }

    public class Image
    {
        public int Id { get; set; }
        public string URl {get;set;}
        public string LocalPath {get;set;}

        public int AdvertId { get; set; }
    }

    public enum RealEstateType
    {
        Flat,
        House
    }

    public enum Usedtype
    {
        New,
        Used
    }

    public enum AdvertType
    {
        Sell,
        RentAnForADay,
        RentAnForAMonth,
    }
}
