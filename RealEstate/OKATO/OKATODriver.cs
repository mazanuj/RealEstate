using RealEstate.Db;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RealEstate.OKATO
{
    [Export(typeof(OKATODriver))]
    public class OKATODriver
    {
        public void Load()
        {
            bool exists = false;
            using (var context = new RealEstateContext())
            {
                exists = context.Database
                     .SqlQuery<int>(@"
                        SELECT COUNT(*)
                        FROM information_schema.tables 
                        WHERE table_schema = 'okato' 
                        AND table_name = 'class_okato';")
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

            string dump = File.ReadAllText(@"OKATO\dump.sql");
            using (var context = new RealEstateContext())
            {
                ((IObjectContextAdapter)context).ObjectContext.CommandTimeout = 36000;
                context.Database.ExecuteSqlCommand(dump);
            }
        }

        public string GetDistinctByCode(string code)
        {
            using (var context = new RealEstateContext())
            {
               return context.Database
                         .SqlQuery<string>(@"
                        SELECT name
                        FROM okato.class_okato
                        WHERE code = {0};", code.Substring(0, 8)).SingleOrDefault();
            }
        }


    }
}
