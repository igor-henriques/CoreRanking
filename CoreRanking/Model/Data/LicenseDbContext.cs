using Microsoft.EntityFrameworkCore;

namespace CoreRanking.Model
{
    class LicenseDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=license.ironside.dev; Port=3306; Database=license; Uid=root; Pwd=95653549Hh*");
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<CoreLicense> CoreLicense { get; set; }
    }
}
