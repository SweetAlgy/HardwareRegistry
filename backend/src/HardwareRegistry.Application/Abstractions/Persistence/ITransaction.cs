namespace HardwareRegistry.Application.Abstractions.Persistence
{
    public interface ITransaction : IAsyncDisposable
    {
        public Task CommitAsync(CancellationToken cancellationToken = default);
        public Task RollbackAsync();
    }
}