import CustomStore from 'devextreme/data/custom_store';
import 'devextreme/ui/button';
import 'devextreme/ui/data_grid';
import { confirm } from 'devextreme/ui/dialog';
import $ from 'jquery';
import { deleteDevice, getDevices } from './api/devices-api.js';
import { DEVICE_CONSTRAINTS } from './constraints.js';
import { notifyError, notifySuccess } from './notifications.js';
import { toPageWindow } from './utils/page-window.js';

const PAGE_SIZES = [10, 20, 50, 100];
const DEFAULT_PAGE_SIZE = 20;
const DEFAULT_SORT_FIELD = 'inventoryNumber';

/**
 * @param {JQuery} $container
 * @param {{ open: (device: object | null) => void }} deviceDialog
 */
export function createDevicesGrid($container, deviceDialog) {
  const $devicesCount = $('<div>').addClass('devices-count');
  let addButton = null;

  const dataSource = new CustomStore({
    key: 'id',
    load: loadDevices,
  });

  const grid = $container
    .dxDataGrid({
      dataSource,
      remoteOperations: { paging: true, sorting: true },
      showBorders: true,
      showRowLines: true,
      rowAlternationEnabled: true,
      hoverStateEnabled: true,
      columnAutoWidth: true,
      errorRowEnabled: false,
      noDataText: 'В базе пока нет техники',
      sorting: { mode: 'single' },
      paging: { pageSize: DEFAULT_PAGE_SIZE },
      pager: {
        visible: true,
        allowedPageSizes: PAGE_SIZES,
        showPageSizeSelector: true,
        showInfo: true,
        showNavigationButtons: true,
      },
      toolbar: {
        items: [
          { location: 'before', template: () => $devicesCount },
          {
            location: 'after',
            widget: 'dxButton',
            options: {
              text: 'Добавить технику',
              icon: 'plus',
              type: 'default',
              stylingMode: 'contained',
              onInitialized: (event) => (addButton = event.component),
              onClick: () => deviceDialog.open(null),
            },
          },
        ],
      },
      columns: [
        { dataField: 'inventoryNumber', caption: 'Учетный номер', sortOrder: 'asc', width: 220 },
        { dataField: 'typeName', caption: 'Тип техники', width: 240 },
        { dataField: 'name', caption: 'Наименование' },
        {
          dataField: 'room',
          caption: 'Комната размещения',
          dataType: 'number',
          alignment: 'left',
          width: 200,
        },
        {
          type: 'buttons',
          width: 100,
          buttons: [
            {
              icon: 'edit',
              hint: 'Редактировать',
              onClick: (event) => deviceDialog.open(event.row.data),
            },
            {
              icon: 'trash',
              hint: 'Удалить',
              onClick: (event) => removeDevice(event.row.data),
            },
          ],
        },
      ],
      onRowDblClick: (event) => {
        if (event.rowType === 'data') deviceDialog.open(event.data);
      },
    })
    .dxDataGrid('instance');

  async function loadDevices(loadOptions) {
    const skip = loadOptions.skip ?? 0;
    const take = loadOptions.take ?? DEFAULT_PAGE_SIZE;
    const sorting = toSorting(loadOptions.sort);
    const { pages, offset } = toPageWindow(skip, take);

    try {
      const devicesPages = await Promise.all(
        pages.map((page) => getDevices({ ...page, ...sorting })),
      );

      const totalCount = devicesPages[0].totalCount;
      const devices = devicesPages.flatMap((devicesPage) => devicesPage.items);

      showTotalCount(totalCount);

      return { data: devices.slice(offset, offset + take), totalCount };
    } catch (error) {
      notifyError('Не удалось загрузить список техники');
      throw error;
    }
  }

  function showTotalCount(totalCount) {
    $devicesCount.text(`Записей: ${totalCount} из ${DEVICE_CONSTRAINTS.maxCount}`);
    addButton?.option('disabled', totalCount >= DEVICE_CONSTRAINTS.maxCount);
  }

  async function removeDevice(device) {
    const isConfirmed = await confirm(
      `Удалить технику «${device.inventoryNumber}»?`,
      'Удаление техники',
    );

    if (!isConfirmed) return;

    try {
      await deleteDevice(device.id);
      notifySuccess(`Техника «${device.inventoryNumber}» удалена`);
    } catch {
      notifyError('Не удалось удалить запись');
    }

    await grid.refresh();
  }

  return {
    refresh: () => grid.refresh(),
  };
}

/** The grid sorts by a single column, so only the first sort descriptor matters. */
function toSorting(sort) {
  const descriptor = Array.isArray(sort) ? sort[0] : sort;

  if (descriptor === null || typeof descriptor !== 'object' || !('selector' in descriptor)) {
    return { sortBy: DEFAULT_SORT_FIELD, sortOrder: 'ascending' };
  }

  return { sortBy: descriptor.selector, sortOrder: descriptor.desc ? 'descending' : 'ascending' };
}
