using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace RealEstate.Exporting
{
    public class ExportSetting
    {
        public int Id { get; set; }

        [Required]
        public ExportSite ExportSite { get; set; }

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

        public bool ReplacePhoneNumber { get; set; }
        public float Margin { get; set; }
        public int Delay { get; set; }
    }
}
