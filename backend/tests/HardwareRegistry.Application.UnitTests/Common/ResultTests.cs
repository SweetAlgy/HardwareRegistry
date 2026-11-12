using HardwareRegistry.Application.Common;
using HardwareRegistry.Application.Common.Errors;

namespace HardwareRegistry.Application.UnitTests.Common
{
    public sealed class ResultTests
    {
        private static readonly Error SampleError = new("Sample.Failed", "Sample failure.", ErrorType.Failure);

        [Fact]
        public void Success_NoValue_IsSuccess()
        {
            var result = Result.Success();

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
        }

        [Fact]
        public void Success_NoValue_ErrorAccessThrows()
        {
            var result = Result.Success();

            Assert.Throws<InvalidOperationException>(() => result.Error);
        }

        [Fact]
        public void Failure_WithError_IsFailureAndCarriesError()
        {
            var result = Result.Failure(ResultTests.SampleError);

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Same(ResultTests.SampleError, result.Error);
        }

        [Fact]
        public void Failure_NoneError_Throws()
            => Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));

        [Fact]
        public void Failure_NullError_Throws()
            => Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));

        [Fact]
        public void SuccessOfT_WithValue_CarriesValue()
        {
            var result = Result.Success(42);

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void FailureOfT_WithError_ValueAccessThrows()
        {
            var result = Result.Failure<int>(ResultTests.SampleError);

            Assert.True(result.IsFailure);
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void FailureOfT_NoneError_Throws()
            => Assert.Throws<InvalidOperationException>(() => Result.Failure<int>(Error.None));

        [Fact]
        public void ImplicitConversion_FromValue_CreatesSuccess()
        {
            Result<string> result = "value";

            Assert.True(result.IsSuccess);
            Assert.Equal("value", result.Value);
        }

        [Fact]
        public void ImplicitConversion_FromErrorToResultOfT_CreatesFailure()
        {
            Result<string> result = ResultTests.SampleError;

            Assert.True(result.IsFailure);
            Assert.Same(ResultTests.SampleError, result.Error);
        }

        [Fact]
        public void ImplicitConversion_FromErrorToResult_CreatesFailure()
        {
            Result result = ResultTests.SampleError;

            Assert.True(result.IsFailure);
            Assert.Same(ResultTests.SampleError, result.Error);
        }
    }
}
