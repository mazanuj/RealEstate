using RealEstate.Db;
using System;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RealEstate.OKATO
{
    [Export(typeof(OKATODriver))]
    public class OKATODriver
    {
        public void Load()
        {
            var exists = false;
            using (var context = new RealEstateContext())
            {
                exists = context.Database
                     .SqlQuery<int>(@"
                        IF DB_ID ('okato') IS NOT NULL
                         select 1;
                        else
                         select 0;")
                     .Single() != 0;
            }

            if(!exists)
            {
                RestoreDefault();
            }
        }
        private void RestoreDefault()
        {
            Trace.WriteLine("Okato table doesn't exist. Creating...");


            var creatingDb = File.ReadAllText(@"OKATO\create.sql");
            using (var context = new RealEstateContext())
            {
                context.Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, creatingDb);
            }

            var dump = File.ReadAllText(@"OKATO\dump.sql");
            using (var context = new RealEstateContext())
            {
                context.Database.ExecuteSqlCommand(dump);
            }
        }

        public string GetDistinctByCode(string code)
        {
            if (String.IsNullOrEmpty(code))
                return null;

            using (var context = new RealEstateContext())
            {
               return context.Database
                         .SqlQuery<string>(@"
                        use okato
                        SELECT name
                        FROM class_okato
                        WHERE code = {0};", code.Length > 8 ? code.Substring(0, 8) : code).FirstOrDefault();
            }
        }

        public string GetCodeByDistinct(string distinct)
        {
            if (String.IsNullOrEmpty(distinct))
                return null;

            using (var context = new RealEstateContext())
            {
                return context.Database
                          .SqlQuery<string>(@"
                        use okato
                        SELECT code
                        FROM class_okato
                        WHERE name = {0};", distinct).FirstOrDefault();
            }
        }

        public string GetParrentCode(string code)
        {
            if (String.IsNullOrEmpty(code))
                return null;

            using (var context = new RealEstateContext())
            {
                return context.Database
                          .SqlQuery<string>(@"
                        use okato
                        SELECT parent_code
                        FROM class_okato
                        WHERE code = {0};", code).FirstOrDefault();
            }
        }


    }
}
