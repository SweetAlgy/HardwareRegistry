/** The largest page size the API accepts. */
export const MAX_PAGE_SIZE = 100;

/**
 * Maps a `skip`/`take` row range onto the API's `page`/`pageSize` paging.
 *
 * The data grid caches rows and asks only for the ones it is missing, so `skip` is often not a
 * multiple of `take`: growing the page size from 20 to 50 asks for rows 20–49. Prefers one page
 * that holds the whole range, the smallest such page size; falls back to several pages of the
 * largest size when no single page fits.
 *
 * @returns {{ pages: { page: number, pageSize: number }[], offset: number }} The pages to load and
 *   where the range starts in their concatenation.
 */
export function toPageWindow(skip, take, maxPageSize = MAX_PAGE_SIZE) {
  const last = skip + take - 1;

  for (let pageSize = take; pageSize <= maxPageSize; pageSize++) {
    const pageIndex = Math.floor(skip / pageSize);

    if (pageIndex === Math.floor(last / pageSize)) {
      return { pages: [{ page: pageIndex + 1, pageSize }], offset: skip - pageIndex * pageSize };
    }
  }

  const firstPageIndex = Math.floor(skip / maxPageSize);
  const lastPageIndex = Math.floor(last / maxPageSize);

  return {
    pages: Array.from({ length: lastPageIndex - firstPageIndex + 1 }, (_, index) => ({
      page: firstPageIndex + index + 1,
      pageSize: maxPageSize,
    })),
    offset: skip - firstPageIndex * maxPageSize,
  };
}
