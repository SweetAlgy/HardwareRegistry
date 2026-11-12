using HardwareRegistry.Application.Common.Exceptions;
using HardwareRegistry.Domain.Devices;

namespace HardwareRegistry.Application.UnitTests.Common.Exceptions
{
    public sealed class PersistenceConflictExceptionTests
    {
        [Fact]
        public void Constructor_WithConstraint_DescribesEntityConstraintAndValues()
        {
            var exception = new PersistenceConflictException(
                typeof(DeviceEntity),
                nameof(DeviceEntity.InventoryNumber),
                "PC1",
                "ix_devices_inventory_number"
            );

            Assert.Equal(
                "DeviceEntity already exists. Constraint: ix_devices_inventory_number. Conflicting values: InventoryNumber: PC1.",
                exception.Message
            );
            Assert.Equal(typeof(DeviceEntity), exception.EntityType);
            Assert.Equal("DeviceEntity", exception.EntityTypeName);
            Assert.Equal("PC1", exception.ConflictingValues[nameof(DeviceEntity.InventoryNumber)]);
            Assert.Equal("ix_devices_inventory_number", exception.ConstraintName);
        }

        [Fact]
        public void Constructor_WithoutConstraint_OmitsConstraintFromMessage()
        {
            var exception = new PersistenceConflictException(typeof(DeviceEntity), "Name", "PC");

            Assert.Equal("DeviceEntity already exists. Conflicting values: Name: PC.", exception.Message);
            Assert.Null(exception.ConstraintName);
        }

        [Fact]
        public void Constructor_InnerException_KeepsIt()
        {
            var inner = new InvalidOperationException();

            var exception = new PersistenceConflictException(typeof(DeviceEntity), "Name", "PC", null, inner);

            Assert.Same(inner, exception.InnerException);
        }
    }
}
