using CoreRanking.Model;
using CoreRanking.Model.RankingPvE;
using CoreRanking.Model.RankingPvP;
using Microsoft.EntityFrameworkCore;
using System;

namespace CoreRanking.Data
{
    public class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionBuilder.GetConnectionString()
                ,new MariaDbServerVersion(new Version(8, 0, 21))
                ,mySqlOptionsAction => mySqlOptionsAction
                .CharSetBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.CharSetBehavior.NeverAppend));

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Collect>().Ignore(t => t.Amount);
            

            base.OnModelCreating(modelBuilder);
        }        

        public DbSet<Role> Role { get; set; }
        public DbSet<Battle> Battle { get; set; }
        public DbSet<Account> Account { get; set; }
        public DbSet<Banned> Banned { get; set; }
        public DbSet<Hunt> Hunt { get; set; }
        public DbSet<Collect> Collect { get; set; }
    }
}
