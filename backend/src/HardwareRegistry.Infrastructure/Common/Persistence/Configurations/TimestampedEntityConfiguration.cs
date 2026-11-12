using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HardwareRegistry.Domain.Common.Abstractions;
using HardwareRegistry.Infrastructure.Persistence.Postgres;

namespace HardwareRegistry.Infrastructure.Common.Persistence.Configurations
{
    public abstract class TimestampedEntityConfiguration<TEntity, T> : EntityConfiguration<TEntity, T>
        where TEntity : class, ITimestampedEntity<T>
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);

            builder.ToTable(table => table.HasCheckConstraint(
                    ConstraintNames.CreatedAtMustNotBeLaterThanUpdatedAt,
                    "created_at <= updated_at"
                )
            );
        }
    }

    public abstract class TimestampedEntityConfiguration<TEntity> : TimestampedEntityConfiguration<TEntity, Guid>
        where TEntity : class, ITimestampedEntity
    { }
}