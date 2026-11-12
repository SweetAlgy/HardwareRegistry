using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Infrastructure.Common.Persistence.Configurations;
using HardwareRegistry.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareRegistry.Infrastructure.Persistence.Configurations
{
    public sealed class DeviceEntityConfiguration : TimestampedEntityConfiguration<DeviceEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DeviceEntity> builder)
        {
            // Natural collation: "PC2" sorts before "PC10".
            builder
                .Property(entity => entity.InventoryNumber)
                .HasMaxLength(DeviceConstants.InventoryNumberMaxLength)
                .UseCollation(CollationNames.Natural)
                .IsRequired();

            builder
                .Property(entity => entity.Name)
                .HasMaxLength(DeviceConstants.NameMaxLength)
                .UseCollation(CollationNames.Natural)
                .IsRequired();

            builder
                .Property(entity => entity.Room)
                .IsRequired();

            builder
                .HasOne(entity => entity.Type)
                .WithMany()
                .HasForeignKey(entity => entity.TypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder
                .HasIndex(entity => entity.InventoryNumber)
                .IsUnique()
                .HasDatabaseName(ConstraintNames.DeviceInventoryNumberIndex);

            builder.ToTable(table =>
                {
                    table.HasCheckConstraint(
                        ConstraintNames.DeviceInventoryNumberFormat,
                        $"inventory_number ~ '{DeviceConstants.InventoryNumberPattern}'"
                    );

                    table.HasCheckConstraint(
                        ConstraintNames.DeviceRoomRange,
                        $"room BETWEEN {DeviceConstants.RoomMin} AND {DeviceConstants.RoomMax}"
                    );
                }
            );
        }
    }
}
