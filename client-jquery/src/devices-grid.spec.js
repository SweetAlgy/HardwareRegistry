import $ from 'jquery';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { userEvent } from 'vitest/browser';
import { deleteDevice, getDevices } from './api/devices-api.js';
import { createDevicesGrid } from './devices-grid.js';
import { notifyError, notifySuccess } from './notifications.js';

vi.mock('./api/devices-api.js', () => ({ getDevices: vi.fn(), deleteDevice: vi.fn() }));
vi.mock('./notifications.js', () => ({ notifySuccess: vi.fn(), notifyError: vi.fn() }));

const ADD_BUTTON = '[aria-label="Добавить технику"]';

const device = {
  id: 'd1',
  inventoryNumber: 'PC1',
  name: 'Рабочая станция',
  typeId: 't1',
  typeName: 'Системный блок',
  room: 101,
};

/** Answers like the server would: as many rows as the query asks for, up to the total. */
function serverPage(totalCount) {
  return async (query) => {
    const skip = (query.page - 1) * query.pageSize;
    const items = Array.from(
      { length: Math.max(0, Math.min(query.pageSize, totalCount - skip)) },
      (_, index) => ({ ...device, id: `d${skip + index}`, inventoryNumber: `PC${skip + index}` }),
    );

    return { items, page: query.page, pageSize: query.pageSize, totalCount };
  };
}

describe('createDevicesGrid', () => {
  let $container;
  let deviceDialog;
  let gridApi;

  async function render() {
    $container = $('<div>').appendTo(document.body);
    gridApi = createDevicesGrid($container, deviceDialog);
    await vi.waitFor(() => expect(getDevices).toHaveBeenCalled());
    await vi.waitFor(() => expect($container.find('.dx-data-row').length).toBeGreaterThan(0));
  }

  function grid() {
    return $container.dxDataGrid('instance');
  }

  function lastQuery() {
    return getDevices.mock.lastCall[0];
  }

  function visibleInventoryNumbers() {
    return grid()
      .getVisibleRows()
      .map((row) => row.data.inventoryNumber);
  }

  function inventoryNumbers(firstRow, rowCount) {
    return Array.from({ length: rowCount }, (_, index) => `PC${firstRow + index}`);
  }

  beforeEach(() => {
    vi.clearAllMocks();
    getDevices.mockImplementation(async () => ({
      items: [device],
      page: 1,
      pageSize: 20,
      totalCount: 1,
    }));
    deleteDevice.mockResolvedValue(undefined);
    deviceDialog = { open: vi.fn() };
  });

  it('render_initialLoad_requestsFirstPageSortedByInventoryNumber', async () => {
    await render();

    expect(lastQuery()).toEqual({
      page: 1,
      pageSize: 20,
      sortBy: 'inventoryNumber',
      sortOrder: 'ascending',
    });
  });

  it('render_loadedPage_showsColumnsRowsAndRecordCount', async () => {
    await render();

    const text = $container.text();
    for (const caption of ['Учетный номер', 'Тип техники', 'Наименование', 'Комната размещения']) {
      expect(text).toContain(caption);
    }
    expect(text).toContain('PC1');
    expect(text).toContain('Системный блок');
    expect(text).toContain('Записей: 1 из 2000');
  });

  it.each([
    ['room', 'desc', 'room', 'descending'],
    ['typeName', 'asc', 'typeName', 'ascending'],
    ['name', 'desc', 'name', 'descending'],
    ['inventoryNumber', 'desc', 'inventoryNumber', 'descending'],
  ])(
    'sort_%s_%s_requestsServerSorting',
    async (dataField, gridOrder, expectedSortBy, expectedSortOrder) => {
      await render();

      grid().clearSorting();
      grid().columnOption(dataField, 'sortOrder', gridOrder);

      await vi.waitFor(() =>
        expect(lastQuery()).toMatchObject({ sortBy: expectedSortBy, sortOrder: expectedSortOrder }),
      );
    },
  );

  it('sort_headerClick_togglesServerSorting', async () => {
    await render();

    await userEvent.click($container.find('.dx-header-row td:contains("Комната размещения")')[0]);

    await vi.waitFor(() =>
      expect(lastQuery()).toMatchObject({ sortBy: 'room', sortOrder: 'ascending' }),
    );
  });

  it('page_pageSizeGrownFrom20To50_showsRows0To49WithoutDuplicates', async () => {
    getDevices.mockImplementation(serverPage(300));
    await render();

    grid().pageSize(50);

    await vi.waitFor(() => expect(visibleInventoryNumbers()).toEqual(inventoryNumbers(0, 50)));
  });

  it('page_sequenceOfPageAndSizeChanges_alwaysShowsTheRightRows', async () => {
    getDevices.mockImplementation(serverPage(300));
    await render();
    const steps = [
      [() => grid().pageSize(50), 0, 50],
      [() => grid().pageIndex(2), 100, 50],
      [() => grid().pageSize(100), 200, 100],
      [() => grid().pageIndex(1), 100, 100],
      [() => grid().pageSize(10), 10, 10],
      [() => grid().pageIndex(4), 40, 10],
      [() => grid().pageSize(20), 80, 20],
    ];

    for (const [change, firstRow, rowCount] of steps) {
      change();

      await vi.waitFor(() =>
        expect(visibleInventoryNumbers()).toEqual(inventoryNumbers(firstRow, rowCount)),
      );
    }
  });

  it('refresh_called_reloadsAndUpdatesRecordCount', async () => {
    await render();
    getDevices.mockImplementation(async () => ({ items: [device], totalCount: 2 }));

    await gridApi.refresh();

    await vi.waitFor(() => expect($container.text()).toContain('Записей: 2 из 2000'));
  });

  it('render_registryFull_disablesAddButton', async () => {
    getDevices.mockImplementation(async () => ({ items: [device], totalCount: 2000 }));
    await render();

    await vi.waitFor(() =>
      expect($container.find(ADD_BUTTON).hasClass('dx-state-disabled')).toBe(true),
    );
  });

  it('render_registryNotFull_enablesAddButton', async () => {
    getDevices.mockImplementation(async () => ({ items: [device], totalCount: 1999 }));
    await render();

    expect($container.find(ADD_BUTTON).hasClass('dx-state-disabled')).toBe(false);
  });

  it('render_loadFails_showsErrorNotification', async () => {
    getDevices.mockRejectedValue(new Error('offline'));
    $container = $('<div>').appendTo(document.body);

    createDevicesGrid($container, deviceDialog);

    await vi.waitFor(() =>
      expect(notifyError).toHaveBeenCalledWith('Не удалось загрузить список техники'),
    );
  });

  it('addButton_click_opensDialogForNewDevice', async () => {
    await render();

    await userEvent.click($container.find(ADD_BUTTON)[0]);

    expect(deviceDialog.open).toHaveBeenCalledWith(null);
  });

  it('row_doubleClick_opensDialogForThatDevice', async () => {
    await render();

    await userEvent.dblClick($container.find('.dx-data-row td')[0]);

    expect(deviceDialog.open).toHaveBeenCalledWith(expect.objectContaining({ id: 'd1' }));
  });

  it('row_editButton_opensDialogForThatDevice', async () => {
    await render();

    await userEvent.click($container.find('.dx-data-row .dx-icon-edit')[0]);

    expect(deviceDialog.open).toHaveBeenCalledWith(expect.objectContaining({ id: 'd1' }));
  });

  it('row_deleteConfirmed_deletesDeviceAndReloads', async () => {
    await render();
    const callsBefore = getDevices.mock.calls.length;

    await userEvent.click($container.find('.dx-data-row .dx-icon-trash')[0]);
    await vi.waitFor(() => expect($('.dx-dialog-button').length).toBe(2));
    expect(document.body.textContent).toContain('Удалить технику «PC1»?');
    await userEvent.click($('.dx-dialog-button')[0]);

    await vi.waitFor(() => expect(deleteDevice).toHaveBeenCalledWith('d1'));
    await vi.waitFor(() => expect(getDevices.mock.calls.length).toBeGreaterThan(callsBefore));
    expect(notifySuccess).toHaveBeenCalledWith('Техника «PC1» удалена');
  });

  it('row_deleteCancelled_keepsDevice', async () => {
    await render();

    await userEvent.click($container.find('.dx-data-row .dx-icon-trash')[0]);
    await vi.waitFor(() => expect($('.dx-dialog-button').length).toBe(2));
    await userEvent.click($('.dx-dialog-button')[1]);

    expect(deleteDevice).not.toHaveBeenCalled();
  });

  it('row_deleteFails_showsErrorAndReloads', async () => {
    deleteDevice.mockRejectedValue(new Error('offline'));
    await render();
    const callsBefore = getDevices.mock.calls.length;

    await userEvent.click($container.find('.dx-data-row .dx-icon-trash')[0]);
    await vi.waitFor(() => expect($('.dx-dialog-button').length).toBe(2));
    await userEvent.click($('.dx-dialog-button')[0]);

    await vi.waitFor(() => expect(notifyError).toHaveBeenCalledWith('Не удалось удалить запись'));
    await vi.waitFor(() => expect(getDevices.mock.calls.length).toBeGreaterThan(callsBefore));
  });
});
