using HardwareRegistry.Application.Common.Errors;

namespace HardwareRegistry.Application.UnitTests.Common.Errors
{
    public sealed class OperationErrorsTests
    {
        [Fact]
        public void LimitExceeded_LimitAndMaximum_ReturnsValidationErrorWithDetails()
        {
            var error = OperationErrors.LimitExceeded("devices", 2000);

            Assert.Equal("Operation.LimitExceeded", error.Code);
            Assert.Equal(ErrorType.Validation, error.Type);
            Assert.Equal("The 'devices' limit has been exceeded.", error.Message);
            Assert.NotNull(error.Details);
            Assert.Equal("devices", error.Details["limit"]);
            Assert.Equal(2000, error.Details["maximum"]);
        }

        [Fact]
        public void ConcurrentModification_ReturnsConflict()
        {
            var error = OperationErrors.ConcurrentModification();

            Assert.Equal("Operation.ConcurrentModification", error.Code);
            Assert.Equal(ErrorType.Conflict, error.Type);
        }
    }
}
