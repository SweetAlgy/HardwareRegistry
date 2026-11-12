using System.Data;

namespace HardwareRegistry.Application.Abstractions.Persistence
{
    public interface IUnitOfWork
    {
        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        public Task<ITransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default
        );

        public Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}