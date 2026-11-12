import { MAX_PAGE_SIZE, toPageWindow } from './page-window';

describe('toPageWindow', () => {
  it('toPageWindow_alignedRange_requestsThatPageAsIs', () => {
    expect(toPageWindow(40, 20)).toEqual({ pages: [{ page: 3, pageSize: 20 }], offset: 0 });
  });

  it('toPageWindow_firstPage_requestsPageOne', () => {
    expect(toPageWindow(0, 20)).toEqual({ pages: [{ page: 1, pageSize: 20 }], offset: 0 });
  });

  it('toPageWindow_pageSizeGrownFrom20To50_requestsOnePageHoldingRows20To49', () => {
    // The grid already has rows 0–19 and asks only for the rest.
    expect(toPageWindow(20, 30)).toEqual({ pages: [{ page: 1, pageSize: 50 }], offset: 20 });
  });

  it('toPageWindow_noSinglePageFits_requestsSeveralLargestPages', () => {
    expect(toPageWindow(150, 100)).toEqual({
      pages: [
        { page: 2, pageSize: 100 },
        { page: 3, pageSize: 100 },
      ],
      offset: 50,
    });
  });

  it('toPageWindow_takeAboveMaximum_splitsIntoLargestPages', () => {
    const window = toPageWindow(0, 250);

    expect(window.pages).toEqual([
      { page: 1, pageSize: 100 },
      { page: 2, pageSize: 100 },
      { page: 3, pageSize: 100 },
    ]);
    expect(window.offset).toBe(0);
  });

  it('toPageWindow_everyRangeUpTo300Rows_returnsExactlyThoseRows', () => {
    const rows = Array.from({ length: 300 }, (_, index) => index);
    const serverPage = (page: number, pageSize: number) =>
      rows.slice((page - 1) * pageSize, page * pageSize);

    for (let skip = 0; skip < rows.length; skip++) {
      for (let take = 1; take <= MAX_PAGE_SIZE && skip + take <= rows.length; take++) {
        const { pages, offset } = toPageWindow(skip, take);
        const fetched = pages.flatMap(({ page, pageSize }) => serverPage(page, pageSize));

        expect(pages.every(({ pageSize }) => pageSize <= MAX_PAGE_SIZE)).toBe(true);
        expect(fetched.slice(offset, offset + take)).toEqual(rows.slice(skip, skip + take));
      }
    }
  });
});
