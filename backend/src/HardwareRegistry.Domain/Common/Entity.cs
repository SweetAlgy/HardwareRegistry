using HardwareRegistry.Domain.Common.Abstractions;

namespace HardwareRegistry.Domain.Common
{
    public abstract class Entity<T> : IEntity<T> where T : notnull
    {
        protected Entity(T id)
        {
            ArgumentNullException.ThrowIfNull(id);
            this.Id = id;
        }

        public T Id { get; private set; }
    }

    public abstract class Entity(Guid id) : Entity<Guid>(id), IEntity
    {
        protected Entity() : this(Guid.CreateVersion7()) { }
    }
}