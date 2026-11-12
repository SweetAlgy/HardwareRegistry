using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Devices;
using HardwareRegistry.Application.Devices.Abstractions;
using HardwareRegistry.Application.Devices.Requests;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Infrastructure.Common.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace HardwareRegistry.Infrastructure.Persistence.Repositories
{
    public sealed class DevicesRepository(ApplicationDbContext context) : RepositoryBase(context), IDevicesRepository
    {
        public void Add(DeviceEntity deviceEntity)
            => this.Context.Devices.Add(deviceEntity);

        public async Task<DeviceEntity?> GetTrackedByIdAsync(
            Guid deviceId,
            CancellationToken cancellationToken = default
        ) => await this.Context.Devices
            .Where(entity => entity.Id == deviceId)
            .FirstOrDefaultAsync(cancellationToken);

        public async Task<DeviceEntity?> GetByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
            => await this.Context.Devices
                .AsNoTracking()
                .Include(entity => entity.Type)
                .Where(entity => entity.Id == deviceId)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<PagedResult<DeviceEntity>> GetPageAsync(
            GetDevicesRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var query = this.Context.Devices
                .AsNoTracking()
                .Include(entity => entity.Type);

            var totalCount = await query.CountAsync(cancellationToken);

            var orderedQuery = request.SortBy switch
            {
                DeviceSortField.InventoryNumber => query.OrderBy(request.SortOrder, entity => entity.InventoryNumber),
                DeviceSortField.TypeName => query.OrderBy(request.SortOrder, entity => entity.Type!.Name),
                DeviceSortField.Name => query.OrderBy(request.SortOrder, entity => entity.Name),
                DeviceSortField.Room => query.OrderBy(request.SortOrder, entity => entity.Room),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.SortBy,
                    "Unsupported device sort field."
                )
            };

            // The id makes the order total, so rows with equal sort values do not jump between pages.
            var items = await orderedQuery
                .ThenBy(entity => entity.Id)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new()
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
            => await this.Context.Devices.CountAsync(cancellationToken);

        public async Task<bool> DeleteByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
        {
            var affectedRows = await this.Context.Devices
                .Where(entity => entity.Id == deviceId)
                .ExecuteDeleteAsync(cancellationToken);

            return affectedRows == 1;
        }
    }
}
