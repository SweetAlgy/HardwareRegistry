using HardwareRegistry.Domain.DeviceTypes;
using HardwareRegistry.Infrastructure.Common.Persistence.Configurations;
using HardwareRegistry.Infrastructure.Persistence.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareRegistry.Infrastructure.Persistence.Configurations
{
    public sealed class DeviceTypeEntityConfiguration : EntityConfiguration<DeviceTypeEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DeviceTypeEntity> builder)
        {
            builder
                .Property(entity => entity.Name)
                .HasMaxLength(DeviceTypeConstants.NameMaxLength)
                .UseCollation(CollationNames.Natural)
                .IsRequired();

            builder
                .HasIndex(entity => entity.Name)
                .IsUnique()
                .HasDatabaseName(ConstraintNames.DeviceTypeNameIndex);
        }
    }
}
