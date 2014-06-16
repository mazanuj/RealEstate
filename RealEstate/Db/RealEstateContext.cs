using System.Data.Entity;
using RealEstate.Exporting;
using RealEstate.Parsing;
using System.ComponentModel.Composition;
using RealEstate.Migrations;

namespace RealEstate.Db
{

    [Export(typeof(RealEstateContext))]
    public class RealEstateContext : DbContext
    {
        public static bool isOk { get; set; }

        static RealEstateContext()
        {
            isOk = false;
            Database.SetInitializer<RealEstateContext>(new MigrateDatabaseToLatestVersion<RealEstateContext, Configuration>());
            //Database.SetInitializer<RealEstateContext>(new DropCreateDatabaseAlways<RealEstateContext>());
        }

        public DbSet<Advert> Adverts { get; set; }
        public DbSet<ParserSetting> ParserSettings { get; set; }
        public DbSet<ParserSourceUrl> SourceUrls { get; set; }
        public DbSet<ExportSite> ExportSites { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<ExportSetting> ExportSettings { get; set; }
        public DbSet<ExportItem> ExportItems { get; set; }

        public RealEstateContext()
            : base("RealEstateContext")
        {
        }

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

            modelBuilder.Entity<Advert>()
                .HasMany(a => a.Images)
                .WithRequired(i => i.Advert)
                .WillCascadeOnDelete();
        }
    }
}
