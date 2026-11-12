using HardwareRegistry.Application.Common.Requests;
using HardwareRegistry.Application.UnitTests.TestUtilities;

namespace HardwareRegistry.Application.UnitTests.Common.Requests
{
    public sealed class SearchRequestTests
    {
        [Theory]
        [InlineData(1, 20, 0)]
        [InlineData(2, 20, 20)]
        [InlineData(5, 50, 200)]
        public void Skip_PageAndPageSize_ReturnsItemsBeforePage(int page, int pageSize, int expectedSkip)
        {
            var request = new SearchRequest { Page = page, PageSize = pageSize };

            Assert.Equal(expectedSkip, request.Skip);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(100, 100)]
        [InlineData(Int32.MaxValue, 20)]
        public void Validate_PageAndPageSizeInRange_IsValid(int page, int pageSize)
            => Assert.Empty(ModelValidator.Validate(new SearchRequest { Page = page, PageSize = pageSize }));

        [Theory]
        [InlineData(0, 20, nameof(SearchRequest.Page))]
        [InlineData(-1, 20, nameof(SearchRequest.Page))]
        [InlineData(1, 0, nameof(SearchRequest.PageSize))]
        [InlineData(1, 101, nameof(SearchRequest.PageSize))]
        public void Validate_OutOfRange_ReportsMember(int page, int pageSize, string expectedMember)
        {
            var errors = ModelValidator.Validate(new SearchRequest { Page = page, PageSize = pageSize });

            var error = Assert.Single(errors);
            Assert.Contains(expectedMember, error.MemberNames);
        }

    }
}
