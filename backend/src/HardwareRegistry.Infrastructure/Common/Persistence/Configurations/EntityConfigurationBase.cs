using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HardwareRegistry.Application.Common.Extensions;
using HardwareRegistry.Domain.Common.Abstractions;

namespace HardwareRegistry.Infrastructure.Common.Persistence.Configurations
{
    /// <summary>
    /// Base EF Core entity configuration that automatically maps the table name and primary key
    /// for any entity implementing <see cref="IEntity"/>.
    /// <para>
    /// The table name is derived from the entity class name: the optional <c>Entity</c> suffix
    /// is stripped, the result is pluralized with Humanizer, and then converted to snake_case.
    /// For example, <c>TypeName</c> → <c>type_names</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TEntity">The entity type to configure.</typeparam>
    /// <typeparam name="T">The type of the entity's primary key.</typeparam>
    public abstract class EntityConfigurationBase<TEntity, T> : IEntityTypeConfiguration<TEntity>
        where TEntity : class, IEntity<T>
    {
        private const string EntityPostfix = "Entity";

        /// <summary>
        /// Applies common table-name and primary-key configuration, then delegates to
        /// <see cref="ConfigureEntity"/> for entity-specific mappings.
        /// </summary>
        /// <param name="builder">The builder used to configure the entity type.</param>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            var entityTypeName = typeof(TEntity).Name;

            if (entityTypeName.EndsWith(EntityConfigurationBase<TEntity, T>.EntityPostfix))
                entityTypeName = entityTypeName[..^EntityConfigurationBase<TEntity, T>.EntityPostfix.Length];

            var tableName = entityTypeName
                .Pluralize()
                .ToSnakeCase();

            builder.ToTable(tableName);

            this.ConfigureEntity(builder);
        }

        /// <summary>
        /// Applies entity-specific column constraints, indexes, and relationships.
        /// Implemented by each concrete configuration class.
        /// </summary>
        /// <param name="builder">The builder used to configure the entity type.</param>
        protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
    }

    public abstract class EntityConfigurationBase<TEntity> : EntityConfigurationBase<TEntity, Guid>
        where TEntity : class, IEntity
    { }
}