import $ from 'jquery';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getDeviceTypes } from './device-types-api.js';
import { createDevice, deleteDevice, getDevices, updateDevice } from './devices-api.js';

describe('devices API', () => {
  let ajax;

  beforeEach(() => {
    ajax = vi.spyOn($, 'ajax').mockReturnValue($.Deferred().resolve({}).promise());
  });

  afterEach(() => vi.restoreAllMocks());

  const dto = { inventoryNumber: 'PC1', name: 'Workstation', typeId: 't1', room: 101 };

  it('getDevices_query_sendsPagingAndSortingInQueryString', () => {
    getDevices({ page: 2, pageSize: 20, sortBy: 'room', sortOrder: 'descending' });

    expect(ajax.mock.lastCall[0]).toMatchObject({
      method: 'GET',
      url: '/api/devices?page=2&pageSize=20&sortBy=room&sortOrder=descending',
    });
  });

  it('createDevice_dto_postsToCollection', () => {
    createDevice(dto);

    expect(ajax.mock.lastCall[0]).toMatchObject({
      method: 'POST',
      url: '/api/devices',
      data: JSON.stringify(dto),
    });
  });

  it('updateDevice_idAndDto_putsToDeviceUrl', () => {
    updateDevice('d1', dto);

    expect(ajax.mock.lastCall[0]).toMatchObject({
      method: 'PUT',
      url: '/api/devices/d1',
      data: JSON.stringify(dto),
    });
  });

  it('deleteDevice_id_sendsDeleteToDeviceUrl', () => {
    deleteDevice('d1');

    expect(ajax.mock.lastCall[0]).toMatchObject({ method: 'DELETE', url: '/api/devices/d1' });
  });

  it('getDeviceTypes_dictionary_getsDeviceTypes', () => {
    getDeviceTypes();

    expect(ajax.mock.lastCall[0]).toMatchObject({ method: 'GET', url: '/api/device-types' });
  });
});
