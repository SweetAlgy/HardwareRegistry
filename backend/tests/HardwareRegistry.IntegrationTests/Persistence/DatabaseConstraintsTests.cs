using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Domain.Devices;
using HardwareRegistry.Domain.DeviceTypes;
using HardwareRegistry.Infrastructure.Persistence;
using HardwareRegistry.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HardwareRegistry.IntegrationTests.Persistence
{
    /// <summary>
    /// The database is the last line of defence: these rules hold even for writes that skip the API.
    /// </summary>
    public sealed class DatabaseConstraintsTests(TestEnvironment environment) : IntegrationTest(environment)
    {
        [Fact]
        public async Task SaveChangesAsync_DuplicateInventoryNumber_ThrowsPersistenceConflict()
        {
            var existing = await this.AddDeviceAsync("PC1");

            var exception = await Assert.ThrowsAsync<PersistenceConflictException>(() =>
                this.SaveThroughUnitOfWorkAsync(context => context.Devices.Add(
                        DatabaseConstraintsTests.CreateDevice("PC1", existing.TypeId)
                    )
                )
            );

            Assert.Equal(typeof(DeviceEntity), exception.EntityType);
            Assert.Equal("ix_devices_inventory_number", exception.ConstraintName);
            Assert.Equal("PC1", exception.ConflictingValues[nameof(DeviceEntity.InventoryNumber)]);
        }

        [Fact]
        public async Task SaveChangesAsync_DuplicateDeviceTypeName_ThrowsPersistenceConflict()
        {
            var existingName = (await this.GetDeviceTypesAsync())[0].Name;

            var exception = await Assert.ThrowsAsync<PersistenceConflictException>(() =>
                this.SaveThroughUnitOfWorkAsync(context => context.DeviceTypes.Add(new() { Name = existingName }))
            );

            Assert.Equal(typeof(DeviceTypeEntity), exception.EntityType);
            Assert.Equal(existingName, exception.ConflictingValues[nameof(DeviceTypeEntity.Name)]);
        }

        [Theory]
        [InlineData("PC-1")]
        [InlineData("PC 1")]
        [InlineData("ПК1")]
        [InlineData("PC1\n")]
        [InlineData("")]
        public async Task Insert_InvalidInventoryNumber_ViolatesFormatCheck(string inventoryNumber)
        {
            var exception = await this.InsertExpectingFailureAsync(inventoryNumber: inventoryNumber);

            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal("ck_devices_inventory_number_format", exception.ConstraintName);
        }

        [Fact]
        public async Task Insert_InventoryNumberLongerThan32_IsRejected()
        {
            var exception = await this.InsertExpectingFailureAsync(inventoryNumber: new string('A', 33));

            Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, exception.SqlState);
        }

        [Fact]
        public async Task Insert_NameLongerThan256_IsRejected()
        {
            var exception = await this.InsertExpectingFailureAsync(name: new string('Я', 257));

            Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, exception.SqlState);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1001)]
        [InlineData(-5)]
        public async Task Insert_RoomOutOfRange_ViolatesRoomCheck(int room)
        {
            var exception = await this.InsertExpectingFailureAsync(room: room);

            Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
            Assert.Equal("ck_devices_room_range", exception.ConstraintName);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000)]
        public async Task Insert_RoomOnBoundary_IsAccepted(int room)
        {
            await this.AddDeviceAsync("PC1", room: room);

            Assert.Equal(1, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Insert_InventoryNumberAndNameOfMaxLength_IsAccepted()
        {
            await this.AddDeviceAsync(new string('A', 32), name: new string('Я', 256));

            Assert.Equal(1, await this.CountDevicesAsync());
        }

        [Fact]
        public async Task Insert_UnknownType_ViolatesForeignKey()
        {
            var exception = await this.InsertExpectingFailureAsync(typeId: Guid.NewGuid());

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        }

        [Fact]
        public async Task Delete_DeviceTypeInUse_IsRestricted()
        {
            var device = await this.AddDeviceAsync("PC1");

            var exception = await Assert.ThrowsAsync<PostgresException>(() => this.WithContextAsync(context =>
                    context.DeviceTypes
                        .Where(type => type.Id == device.TypeId)
                        .ExecuteDeleteAsync(IntegrationTest.CancellationToken)
                )
            );

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        }

        private async Task<PostgresException> InsertExpectingFailureAsync(
            string inventoryNumber = "PC1",
            string name = "Workstation",
            int room = 1,
            Guid? typeId = null
        )
        {
            typeId ??= (await this.GetDeviceTypesAsync())[0].Id;

            // Raw SQL, so EF's own length checks cannot get in the way of the database's.
            var exception = await Assert.ThrowsAsync<PostgresException>(() => this.WithContextAsync(context =>
                    context.Database.ExecuteSqlInterpolatedAsync(
                        $"""
                        INSERT INTO devices (id, inventory_number, name, type_id, room, created_at, updated_at)
                        VALUES ({Guid.NewGuid()}, {inventoryNumber}, {name}, {typeId}, {room}, now(), now())
                        """,
                        IntegrationTest.CancellationToken
                    )
                )
            );

            Assert.Equal(0, await this.CountDevicesAsync());
            return exception;
        }

        private async Task SaveThroughUnitOfWorkAsync(Action<ApplicationDbContext> change)
        {
            await using var context = this.Environment.CreateDbContext();
            change(context);
            await new UnitOfWork(context).SaveChangesAsync(IntegrationTest.CancellationToken);
        }

        private static DeviceEntity CreateDevice(string inventoryNumber, Guid typeId)
            => new(DateTime.UtcNow)
            {
                InventoryNumber = inventoryNumber,
                Name = "Workstation",
                TypeId = typeId,
                Room = 1
            };
    }
}
