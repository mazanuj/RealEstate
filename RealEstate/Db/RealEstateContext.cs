using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using MySql.Data.MySqlClient;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System.ComponentModel.Composition;

namespace RealEstate.Db
{
    [Export(typeof(RealEstateContext))]
    public class RealEstateContext : DbContext
    {
        public static bool isOk { get; set; }

        static RealEstateContext()
        {
            isOk = false;
        }

        public DbSet<Advert> Adverts { get; set; }
        public DbSet<ParserSetting> ParserSettings { get; set; }
        public DbSet<ParserSourceUrl> ParserSourceUrls { get; set; }
        public DbSet<ExportSite> ExportSites { get; set; }
        public DbSet<Image> Images { get; set; }

        public RealEstateContext()
            : base("RealEstateContext")
        { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExportSite>()
                .HasMany(e => e.ParseSettings)
                .WithRequired(e => e.ExportSite)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ParserSetting>()
                .HasMany(p => p.Urls)
                .WithRequired(u => u.ParserSetting)
                .WillCascadeOnDelete(true);
        }
    }
}
