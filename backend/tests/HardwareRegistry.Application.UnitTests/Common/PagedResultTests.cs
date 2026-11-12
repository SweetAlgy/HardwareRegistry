using HardwareRegistry.Application.Common;

namespace HardwareRegistry.Application.UnitTests.Common
{
    public sealed class PagedResultTests
    {
        [Theory]
        [InlineData(0, 20, 0)]
        [InlineData(1, 20, 1)]
        [InlineData(20, 20, 1)]
        [InlineData(21, 20, 2)]
        [InlineData(2000, 20, 100)]
        public void TotalPages_TotalCountAndPageSize_RoundsUp(int totalCount, int pageSize, int expectedTotalPages)
        {
            var page = new PagedResult<int> { Page = 1, PageSize = pageSize, TotalCount = totalCount };

            Assert.Equal(expectedTotalPages, page.TotalPages);
        }

        [Theory]
        [InlineData(1, false, true)]
        [InlineData(2, true, true)]
        [InlineData(3, true, false)]
        public void HasPreviousAndNextPage_MiddleAndEdges_ReflectPosition(
            int pageNumber,
            bool expectedHasPrevious,
            bool expectedHasNext
        )
        {
            var page = new PagedResult<int> { Page = pageNumber, PageSize = 10, TotalCount = 25 };

            Assert.Equal(expectedHasPrevious, page.HasPreviousPage);
            Assert.Equal(expectedHasNext, page.HasNextPage);
        }

        [Fact]
        public void To_ProjectedItems_KeepsPaginationMetadata()
        {
            var page = new PagedResult<int> { Items = [1, 2], Page = 2, PageSize = 2, TotalCount = 5 };

            var projected = page.To(page.Items.Select(item => $"#{item}"));

            Assert.Equal(["#1", "#2"], projected.Items);
            Assert.Equal(2, projected.Page);
            Assert.Equal(2, projected.PageSize);
            Assert.Equal(5, projected.TotalCount);
        }
    }
}
