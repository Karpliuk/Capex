using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Capex.Models;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Capex.Models
{
    public class CapexContext : DbContext
    {
        public DbSet<Request> Requests { get; set; }
        public DbSet<User> Users { get; set; }

        public CapexContext()
            : base("CapexDB")
        {
           // Database.SetInitializer(new DropCreateDatabaseIfModelChanges<CapexContext>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
           modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
        
    }
}