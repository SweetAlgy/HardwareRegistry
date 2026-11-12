using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using HardwareRegistry.Infrastructure.Persistence;

namespace HardwareRegistry.DatabaseMigrator
{
    public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql()
                .UseSnakeCaseNamingConvention()
                .Options;

            return new(options);
        }
    }
}
