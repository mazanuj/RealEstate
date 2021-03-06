﻿using System;
using System.Collections.Generic;
using RealEstate.Exporting;
using System.ComponentModel.DataAnnotations.Schema;

namespace RealEstate.Parsing
{
    public class ParserSetting
    {

        public ParserSetting()
        {
            Urls = new List<ParserSourceUrl>();
        }

        public int Id { get; set; }

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

        public virtual ExportSite ExportSite {get; set;}
        public virtual ICollection<ParserSourceUrl> Urls { get; set; }
      
        public static DateTime GetDate(ParsePeriod period)
        {
            switch (period)
            {
                case ParsePeriod.Today:
                    return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(0);
                case ParsePeriod.Yesterday:
                    return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1);
                case ParsePeriod.Week:
                    return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-6);
                case ParsePeriod.All:
                    return DateTime.MinValue;
                default:
                    return DateTime.MinValue;
            }
        }

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
        All,
        Custom
    }
}
