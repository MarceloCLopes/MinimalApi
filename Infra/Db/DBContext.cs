using Microsoft.EntityFrameworkCore;
using MinimalApp.Domain.Entities;

namespace MinimalApp.Infra.Db
{
    public class DBContext : DbContext
    {
        private readonly IConfiguration _configurationAppSettings;

        public DBContext(IConfiguration configurationAppSettings)
        {
            _configurationAppSettings = configurationAppSettings;
        }
        public DbSet<Administrator> Administrators { get; set; } = default!;
        public DbSet<Vehicle> Vehicles { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = _configurationAppSettings.GetConnectionString("Mysql")?.ToString();
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseMySql(
                        connectionString,
                        ServerVersion.AutoDetect(connectionString)
                    );
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Administrator>().HasData(
                new Administrator
                {
                    Id = 1,
                    Email = "administrador@teste.com",
                    Senha = "123456",
                    Perfil = "Adm",
                }
            );
        }
    }
}