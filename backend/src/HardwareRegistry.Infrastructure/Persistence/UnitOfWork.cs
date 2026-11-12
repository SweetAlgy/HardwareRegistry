using System.Data;
using Microsoft.EntityFrameworkCore;
using HardwareRegistry.Application.Abstractions.Persistence;
using HardwareRegistry.Infrastructure.Persistence.Postgres;
using HardwareRegistry.Infrastructure.Persistence.Transactions;

namespace HardwareRegistry.Infrastructure.Persistence
{
    public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await this._context.Database.BeginTransactionAsync(cancellationToken);
            return new EfTransaction(transaction);
        }

        public async Task<ITransaction> BeginTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default
        )
        {
            var transaction = await this._context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            return new EfTransaction(transaction);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await this._context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                throw PostgresExceptionMapper.Map(exception);
            }
        }
    }
}