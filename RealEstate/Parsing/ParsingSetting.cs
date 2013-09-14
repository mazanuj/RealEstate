﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using RealEstate.Validation;

namespace RealEstate.Parsing
{
    public class ParserSetting
    {
        public int Id { get; set; }

        public string City { get; set; }

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

        [Column("ParsePeriod", TypeName = "int")]
        public int ParsePeriodValue { get; set; }
        [NotMapped]
        public ParsePeriod ParsePeriod
        {
            get { return (ParsePeriod)ParsePeriodValue; }
            set { ParsePeriodValue = (int)value; }
        }

        [Column("ImportSite", TypeName = "int")]
        public int ImportSiteValue { get; set; }
        [NotMapped]
        public ImportSite ImportSite
        {
            get { return (ImportSite)ImportSiteValue; }
            set { ImportSiteValue = (int)value; }
        }

        public ExportSite ExportSite {get; set;}
        public virtual ICollection<ParserSourceUrl> Urls { get; set; }

    }

    public class ParserSourceUrl
    {
        public int Id {get; set;}
        public string Url { get; set; }

        public ParserSetting ParserSetting { get; set; }
    }

    public enum ParsePeriod
    {
        Today,
        Yesterday,
        Week,
        All
    }
}
