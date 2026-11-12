using Microsoft.EntityFrameworkCore.Storage;
using HardwareRegistry.Application.Abstractions.Persistence;

namespace HardwareRegistry.Infrastructure.Persistence.Transactions
{
    public sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        private readonly IDbContextTransaction _transaction = transaction;
        private bool _completed;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (this._completed)
                return;

            await this._transaction.CommitAsync(cancellationToken);
            this._completed = true;
        }

        public async Task RollbackAsync()
        {
            if (this._completed)
                return;

            await this._transaction.RollbackAsync(CancellationToken.None);
            this._completed = true;
        }

        public async ValueTask DisposeAsync() { await this._transaction.DisposeAsync(); }
    }
}