using System.ComponentModel.DataAnnotations;

namespace HardwareRegistry.Application.Common.Requests
{
    /// <summary>
    /// Carries the parameters needed to request a single page of results.
    /// </summary>
    public record SearchRequest
    {
        /// <summary>Gets the 1-based page number to retrieve. Defaults to <c>1</c>.</summary>
        [Range(1, Int32.MaxValue)]
        public int Page { get; init; } = 1;

        /// <summary>Gets the maximum number of items to return per page. Defaults to <c>20</c>.</summary>
        [Range(1, 100)]
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Gets the number of items to skip before the current page, computed as
        /// <c>(<see cref="Page"/> - 1) * <see cref="PageSize"/></c>.
        /// </summary>
        public int Skip => (this.Page - 1) * this.PageSize;
    }
}
