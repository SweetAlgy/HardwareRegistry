using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using HardwareRegistry.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;

namespace HardwareRegistry.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<DeviceEntity> Devices => this.Set<DeviceEntity>();
        public DbSet<DeviceTypeEntity> DeviceTypes => this.Set<DeviceTypeEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasCollation(
                CollationNames.Natural,
                provider: "icu",
                locale: "und-u-kn-true",
                deterministic: true
            );

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
