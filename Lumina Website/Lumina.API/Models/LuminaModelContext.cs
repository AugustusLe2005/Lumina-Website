using Microsoft.EntityFrameworkCore;

namespace Lumina.API.Models
{
    public class LuminaModelContext : DbContext
    {
        public LuminaModelContext() { }

        public LuminaModelContext(DbContextOptions<LuminaModelContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<BaiHat> BaiHats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Chuỗi kết nối thẳng tới SQL Server LocalDB WebMusicDB của bạn
                optionsBuilder.UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=WebMusicDB;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.ToTable("Accounts");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<BaiHat>(entity =>
            {
                entity.ToTable("BaiHat");
                entity.HasKey(e => e.Id);
            });
        }
    }
}