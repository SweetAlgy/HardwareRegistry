using HardwareRegistry.Infrastructure.Persistence.Postgres;
using HardwareRegistry.IntegrationTests.Infrastructure;

namespace HardwareRegistry.IntegrationTests.Persistence
{
    public sealed class PostgresDeviceRegistryLockTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task AcquireAsync_NoTransaction_Throws()
        {
            await using var context = this.Environment.CreateDbContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new PostgresDeviceRegistryLock(context).AcquireAsync(IntegrationTest.CancellationToken)
            );
        }

        [Fact]
        public async Task AcquireAsync_HeldByAnotherTransaction_WaitsUntilItEnds()
        {
            await using var firstContext = this.Environment.CreateDbContext();
            await using var secondContext = this.Environment.CreateDbContext();

            await using var firstTransaction = await firstContext.Database.BeginTransactionAsync(IntegrationTest.CancellationToken);
            await new PostgresDeviceRegistryLock(firstContext).AcquireAsync(IntegrationTest.CancellationToken);

            await using var secondTransaction = await secondContext.Database.BeginTransactionAsync(IntegrationTest.CancellationToken);
            var secondAcquire = new PostgresDeviceRegistryLock(secondContext).AcquireAsync(IntegrationTest.CancellationToken);

            await Task.Delay(TimeSpan.FromMilliseconds(500), IntegrationTest.CancellationToken);
            Assert.False(secondAcquire.IsCompleted, "The lock must block while another transaction holds it.");

            await firstTransaction.CommitAsync(IntegrationTest.CancellationToken);

            await secondAcquire.WaitAsync(TimeSpan.FromSeconds(10), IntegrationTest.CancellationToken);
        }

        [Fact]
        public async Task AcquireAsync_SameTransactionTwice_DoesNotBlock()
        {
            await using var context = this.Environment.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync(IntegrationTest.CancellationToken);
            var deviceRegistryLock = new PostgresDeviceRegistryLock(context);

            await deviceRegistryLock.AcquireAsync(IntegrationTest.CancellationToken);
            await deviceRegistryLock
                .AcquireAsync(IntegrationTest.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(5), IntegrationTest.CancellationToken);
        }
    }
}
