namespace HRMS.Shared.Common
{
    /// <summary>
    /// Represents a paginated list of items with metadata about the current page.
    /// </summary>
    /// <typeparam name="T">The type of items in the page.</typeparam>
    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
            => new(Array.Empty<T>(), 0, pageNumber, pageSize);
    }
}
