import { describe, expect, it } from 'vitest';
import { MAX_PAGE_SIZE, toPageWindow } from './page-window.js';

describe('toPageWindow', () => {
  it('toPageWindow_alignedRange_requestsThatPageAsIs', () => {
    expect(toPageWindow(40, 20)).toEqual({ pages: [{ page: 3, pageSize: 20 }], offset: 0 });
  });

  it('toPageWindow_pageSizeGrownFrom20To50_requestsOnePageHoldingRows20To49', () => {
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

  it('toPageWindow_everyRangeUpTo300Rows_returnsExactlyThoseRows', () => {
    const rows = Array.from({ length: 300 }, (_, index) => index);
    const serverPage = (page, pageSize) => rows.slice((page - 1) * pageSize, page * pageSize);

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
