using HardwareRegistry.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HardwareRegistry.IntegrationTests.Persistence
{
    public sealed class MigrationsTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task Migrations_CurrentModel_HasNoPendingChanges()
        {
            var hasPendingModelChanges = await this.WithContextAsync(context =>
                Task.FromResult(context.Database.HasPendingModelChanges())
            );

            Assert.False(hasPendingModelChanges, "The model changed: add a migration.");
        }

        [Fact]
        public async Task Migrations_AppliedDatabase_HasNoPendingMigrations()
        {
            var pendingMigrations = await this.WithContextAsync(context =>
                context.Database.GetPendingMigrationsAsync(IntegrationTest.CancellationToken)
            );

            Assert.Empty(pendingMigrations);
        }
    }
}
