namespace HardwareRegistry.Infrastructure.Persistence.Repositories
{
    public abstract class RepositoryBase(ApplicationDbContext context)
    {
        protected ApplicationDbContext Context { get; } = context;
    }
}