using HardwareRegistry.Application.DeviceTypes.Abstractions;
using HardwareRegistry.Domain.DeviceTypes;
using Microsoft.EntityFrameworkCore;

namespace HardwareRegistry.Infrastructure.Persistence.Repositories
{
    public sealed class DeviceTypesRepository(ApplicationDbContext context)
        : RepositoryBase(context), IDeviceTypesRepository
    {
        public async Task<IReadOnlyList<DeviceTypeEntity>> GetAllAsync(CancellationToken cancellationToken = default)
            => await this.Context.DeviceTypes
                .AsNoTracking()
                .OrderBy(entity => entity.Name)
                .ToListAsync(cancellationToken);

        public async Task<DeviceTypeEntity?> GetTrackedByIdAsync(
            Guid deviceTypeId,
            CancellationToken cancellationToken = default
        ) => await this.Context.DeviceTypes
            .Where(entity => entity.Id == deviceTypeId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
