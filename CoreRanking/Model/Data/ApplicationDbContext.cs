using CoreRanking.Model;
using CoreRanking.Model.Data;
using CoreRanking.Model.RankingPvE;
using Microsoft.EntityFrameworkCore;

namespace CoreRanking.Models
{
    class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionBuilder.GetConnectionString());
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().Ignore(t => t.Evento);
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
