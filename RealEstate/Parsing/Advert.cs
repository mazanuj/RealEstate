using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealEstate.Exporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstate.Parsing
{
    public class AdvertHeader
    {
        public string Url { get; set; }
        public DateTime DateSite { get; set; }
    }

    public class Advert
    {
        public Advert()
        {
            Usedtype = Parsing.Usedtype.Used;
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
        public string MetroStation { get; set; } //+

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

        public bool isGold { get; set; }

        public bool isUnique { get; set; }

        public string Tag { get; set; }

        public DateTime DateSite{ get; set; } //+
        public DateTime DateUpdate { get; set; } //+

        public virtual ICollection<Image> Images { get; set; } //+
        public virtual ICollection<ExportSite> ExportSites { get; set; }

        public override string ToString()
        {
            return String.Format(
                "Rooms: '{0}', Area: '{1:#.0#}', Floor: '{2}', Floor total: '{3}', Seller: '{4}', "
              + "City: '{5}' , Adress: '{6}', Metro: '{7}', AdverType: '{8}', UsedType: '{9}',Price: '{10}',"
              + "Phone: '{11}', RealEstateType: '{12}'", 
                this.Rooms, this.AreaFull, this.Floor, this.FloorTotal, this.Name, 
                this.City, this.Address, this.MetroStation, this.AdvertType, this.Usedtype ,
                this.Price, this.PhoneNumber, this.RealEstateType );
        }

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
