using HardwareRegistry.Api.Extensions;
using HardwareRegistry.Application.Common.Errors;

namespace HardwareRegistry.Api.UnitTests.Extensions
{
    public sealed class ErrorTypeExtensionsTests
    {
        [Theory]
        [InlineData(ErrorType.None, 200)]
        [InlineData(ErrorType.Validation, 400)]
        [InlineData(ErrorType.Unauthorized, 401)]
        [InlineData(ErrorType.Forbidden, 403)]
        [InlineData(ErrorType.NotFound, 404)]
        [InlineData(ErrorType.Conflict, 409)]
        [InlineData(ErrorType.Gone, 410)]
        [InlineData(ErrorType.PreconditionFailed, 412)]
        [InlineData(ErrorType.PayloadTooLarge, 413)]
        [InlineData(ErrorType.UnsupportedMediaType, 415)]
        [InlineData(ErrorType.RateLimited, 429)]
        [InlineData(ErrorType.Failure, 500)]
        [InlineData(ErrorType.Unavailable, 503)]
        [InlineData(ErrorType.Timeout, 504)]
        public void ToStatusCode_ErrorType_ReturnsMatchingStatus(ErrorType errorType, int expectedStatusCode)
            => Assert.Equal(expectedStatusCode, errorType.ToStatusCode());

        [Fact]
        public void ToStatusCode_EveryDefinedErrorType_IsMapped()
            => Assert.All(Enum.GetValues<ErrorType>(), errorType => errorType.ToStatusCode());

        [Fact]
        public void ToTitleString_EveryDefinedErrorType_IsNotEmpty()
            => Assert.All(
                Enum.GetValues<ErrorType>(),
                errorType => Assert.False(String.IsNullOrWhiteSpace(errorType.ToTitleString()))
            );

        [Fact]
        public void ToStatusCode_UndefinedErrorType_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => ((ErrorType)999).ToStatusCode());

        [Fact]
        public void ToTitleString_UndefinedErrorType_Throws()
            => Assert.Throws<ArgumentOutOfRangeException>(() => ((ErrorType)999).ToTitleString());
    }
}
