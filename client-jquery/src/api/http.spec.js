import $ from 'jquery';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { request, toProblemDetails } from './http.js';

describe('request', () => {
  afterEach(() => vi.restoreAllMocks());

  it('request_withBody_sendsJson', () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue($.Deferred().resolve().promise());

    request('POST', '/api/devices', { inventoryNumber: 'PC1', room: 1 });

    expect(ajax).toHaveBeenCalledWith({
      url: '/api/devices',
      method: 'POST',
      contentType: 'application/json',
      data: '{"inventoryNumber":"PC1","room":1}',
    });
  });

  it('request_withoutBody_sendsNoData', () => {
    const ajax = vi.spyOn($, 'ajax').mockReturnValue($.Deferred().resolve().promise());

    request('DELETE', '/api/devices/d1');

    expect(ajax.mock.lastCall[0].data).toBeUndefined();
  });
});

describe('toProblemDetails', () => {
  it('toProblemDetails_problemBody_returnsIt', () => {
    const body = { status: 409, title: 'Conflict', code: 'Device.AlreadyExists' };

    expect(toProblemDetails({ status: 409, responseJSON: body })).toBe(body);
  });

  it.each([
    ['no body', { status: 0 }],
    ['text body', { status: 502, responseJSON: 'Bad gateway' }],
    ['null body', { status: 500, responseJSON: null }],
    ['body without status', { status: 500, responseJSON: { message: 'x' } }],
  ])('toProblemDetails_%s_returnsNull', (_, jqXhr) => {
    expect(toProblemDetails(jqXhr)).toBeNull();
  });

  it('toProblemDetails_notAnXhr_returnsNull', () => {
    expect(toProblemDetails(undefined)).toBeNull();
    expect(toProblemDetails(new Error('boom'))).toBeNull();
  });
});
