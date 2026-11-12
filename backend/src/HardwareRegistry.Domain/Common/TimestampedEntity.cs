using HardwareRegistry.Domain.Common.Abstractions;

namespace HardwareRegistry.Domain.Common
{
    public abstract class TimestampedEntity<T> : Entity<T>, ITimestampedEntity<T> where T : notnull
    {
        protected TimestampedEntity(T id, DateTime createdAt, DateTime updatedAt) : base(id)
        {
            if (createdAt > updatedAt)
                throw new ArgumentException("Updated at cannot be earlier than created at.");

            this.CreatedAt = createdAt;
            this.UpdatedAt = updatedAt;
        }

        protected TimestampedEntity(T id, DateTime createdAt) : this(id, createdAt, createdAt) { }

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        public void MarkUpdated(DateTime updatedAt)
        {
            if (this.CreatedAt > updatedAt)
                throw new ArgumentException("Updated at cannot be earlier than created at.");

            this.UpdatedAt = updatedAt;
        }
    }

    public abstract class TimestampedEntity : TimestampedEntity<Guid>, ITimestampedEntity
    {
        protected TimestampedEntity(Guid id, DateTime createdAt, DateTime updatedAt) : base(id, createdAt, updatedAt) { }
        protected TimestampedEntity(Guid id, DateTime createdAt) : base(id, createdAt) { }
        protected TimestampedEntity(DateTime createdAt) : base(Guid.CreateVersion7(), createdAt) { }
    }
}