using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Domain.UnitTests.Common
{
    public sealed class TimestampedEntityTests
    {
        private static readonly DateTime CreatedAt = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Constructor_CreatedAtOnly_SetsUpdatedAtToCreatedAt()
        {
            var device = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);

            Assert.Equal(TimestampedEntityTests.CreatedAt, device.CreatedAt);
            Assert.Equal(TimestampedEntityTests.CreatedAt, device.UpdatedAt);
        }

        [Fact]
        public void Constructor_CreatedAtOnly_GeneratesVersion7Id()
        {
            var device = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);

            Assert.NotEqual(Guid.Empty, device.Id);
            Assert.Equal(7, device.Id.Version);
        }

        [Fact]
        public void Constructor_TwoEntities_GeneratesDistinctIds()
        {
            var first = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);
            var second = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);

            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Constructor_ExplicitIdAndTimestamps_KeepsThem()
        {
            var id = Guid.NewGuid();
            var updatedAt = TimestampedEntityTests.CreatedAt.AddHours(1);

            var device = new DeviceEntity(id, TimestampedEntityTests.CreatedAt, updatedAt)
            {
                InventoryNumber = "PC1",
                Name = "Workstation",
                TypeId = Guid.NewGuid(),
                Room = 1
            };

            Assert.Equal(id, device.Id);
            Assert.Equal(TimestampedEntityTests.CreatedAt, device.CreatedAt);
            Assert.Equal(updatedAt, device.UpdatedAt);
        }

        [Fact]
        public void Constructor_UpdatedAtEarlierThanCreatedAt_Throws()
        {
            var updatedAt = TimestampedEntityTests.CreatedAt.AddTicks(-1);

            Assert.Throws<ArgumentException>(() => new DeviceEntity(
                    Guid.NewGuid(),
                    TimestampedEntityTests.CreatedAt,
                    updatedAt
                )
                {
                    InventoryNumber = "PC1",
                    Name = "Workstation",
                    TypeId = Guid.NewGuid(),
                    Room = 1
                }
            );
        }

        [Fact]
        public void MarkUpdated_LaterTime_SetsUpdatedAt()
        {
            var device = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);
            var updatedAt = TimestampedEntityTests.CreatedAt.AddMinutes(5);

            device.MarkUpdated(updatedAt);

            Assert.Equal(updatedAt, device.UpdatedAt);
            Assert.Equal(TimestampedEntityTests.CreatedAt, device.CreatedAt);
        }

        [Fact]
        public void MarkUpdated_SameTimeAsCreatedAt_SetsUpdatedAt()
        {
            var device = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);

            device.MarkUpdated(TimestampedEntityTests.CreatedAt);

            Assert.Equal(TimestampedEntityTests.CreatedAt, device.UpdatedAt);
        }

        [Fact]
        public void MarkUpdated_EarlierThanCreatedAt_ThrowsAndKeepsUpdatedAt()
        {
            var device = TimestampedEntityTests.CreateDevice(TimestampedEntityTests.CreatedAt);

            Assert.Throws<ArgumentException>(() => device.MarkUpdated(TimestampedEntityTests.CreatedAt.AddSeconds(-1)));
            Assert.Equal(TimestampedEntityTests.CreatedAt, device.UpdatedAt);
        }

        private static DeviceEntity CreateDevice(DateTime createdAt)
            => new(createdAt)
            {
                InventoryNumber = "PC1",
                Name = "Workstation",
                TypeId = Guid.NewGuid(),
                Room = 1
            };
    }
}
