using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HardwareRegistry.Domain.Common.Abstractions;

namespace HardwareRegistry.Infrastructure.Common.Persistence.Configurations
{
    public abstract class EntityConfiguration<TEntity, T> : EntityConfigurationBase<TEntity, T>
        where TEntity : class, IEntity<T>
    {
        public override void Configure(EntityTypeBuilder<TEntity> builder)
        {
            base.Configure(builder);

            builder.HasKey(entity => entity.Id);

            builder
                .Property(entity => entity.Id)
                .ValueGeneratedNever();
        }
    }

    public abstract class EntityConfiguration<TEntity> : EntityConfiguration<TEntity, Guid>
        where TEntity : class, IEntity
    { }
}