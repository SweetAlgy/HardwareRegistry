import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DxDataGridComponent } from 'devextreme-angular/ui/data-grid';
import { Observable, Subject, of, throwError } from 'rxjs';
import { Mock } from 'vitest';
import { userEvent } from 'vitest/browser';
import { PagedResult } from '../../../../core/models/paged-result.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { Device, DevicesQuery } from '../../models/device.model';
import { DeviceTypesService } from '../../services/device-types.service';
import { DevicesHubService } from '../../services/devices-hub.service';
import { DevicesService } from '../../services/devices.service';
import { DevicesTable } from './devices-table';

const ADD_BUTTON = '[aria-label="Добавить технику"]';

describe('DevicesTable', () => {
  const device: Device = {
    id: 'd1',
    createdAt: '2026-11-12T00:00:00Z',
    updatedAt: '2026-11-12T00:00:00Z',
    inventoryNumber: 'PC1',
    name: 'Рабочая станция',
    typeId: 't1',
    typeName: 'Системный блок',
    room: 101,
  };

  let fixture: ComponentFixture<DevicesTable>;
  let getDevices: Mock<(query: DevicesQuery) => Observable<PagedResult<Device>>>;
  let deleteDevice: Mock;
  let notifications: { success: Mock; error: Mock };
  let hubChanges: Subject<void>;

  function page(totalCount: number, items: Device[] = [device]): PagedResult<Device> {
    return { items, page: 1, pageSize: 20, totalCount, totalPages: 1 } as PagedResult<Device>;
  }

  /** Answers like the server would: as many rows as the query asks for, up to the total. */
  function serverPage(
    totalCount: number,
  ): (query: DevicesQuery) => Observable<PagedResult<Device>> {
    return (query) => {
      const skip = (query.page - 1) * query.pageSize;
      const items = Array.from(
        { length: Math.max(0, Math.min(query.pageSize, totalCount - skip)) },
        (_, index) => ({ ...device, id: `d${skip + index}`, inventoryNumber: `PC${skip + index}` }),
      );

      return of({ ...page(totalCount, items), page: query.page, pageSize: query.pageSize });
    };
  }

  async function render(): Promise<void> {
    fixture = TestBed.createComponent(DevicesTable);
    fixture.detectChanges();
    await vi.waitFor(() => expect(getDevices).toHaveBeenCalled());
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function grid(): DxDataGridComponent {
    return fixture.debugElement.query(By.directive(DxDataGridComponent)).componentInstance;
  }

  function lastQuery(): DevicesQuery {
    return getDevices.mock.lastCall![0];
  }

  function visibleInventoryNumbers(): string[] {
    return grid()
      .instance.getVisibleRows()
      .map((row) => (row.data as Device).inventoryNumber);
  }

  function inventoryNumbers(firstRow: number, rowCount: number): string[] {
    return Array.from({ length: rowCount }, (_, index) => `PC${firstRow + index}`);
  }

  function text(): string {
    return fixture.nativeElement.textContent;
  }

  beforeEach(() => {
    getDevices = vi.fn(() => of(page(1)));
    deleteDevice = vi.fn(() => of(undefined));
    notifications = { success: vi.fn(), error: vi.fn() };
    hubChanges = new Subject<void>();

    TestBed.configureTestingModule({
      imports: [DevicesTable],
      providers: [
        { provide: DevicesService, useValue: { getDevices, deleteDevice } },
        { provide: DeviceTypesService, useValue: { getDeviceTypes: () => of([]) } },
        { provide: DevicesHubService, useValue: { changes$: hubChanges } },
        { provide: NotificationService, useValue: notifications },
      ],
    });
  });

  afterEach(() => {
    fixture?.destroy();
    // Dialogs live in <body>, outside the fixture, and may still be animating out.
    document.querySelectorAll('.dx-overlay-wrapper').forEach((overlay) => overlay.remove());
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

  it('render_loadedPage_showsRowsAndRecordCount', async () => {
    await render();

    await vi.waitFor(() => expect(text()).toContain('PC1'));
    expect(text()).toContain('Системный блок');
    expect(text()).toContain('Рабочая станция');
    expect(text()).toContain('Записей: 1 из 2000');
  });

  it('render_allColumns_showsSpecifiedHeaders', async () => {
    await render();

    for (const caption of ['Учетный номер', 'Тип техники', 'Наименование', 'Комната размещения']) {
      expect(text()).toContain(caption);
    }
  });

  it.each([
    ['room', 'desc', 'room', 'descending'],
    ['typeName', 'asc', 'typeName', 'ascending'],
    ['name', 'desc', 'name', 'descending'],
    ['inventoryNumber', 'desc', 'inventoryNumber', 'descending'],
  ] as const)(
    'sort_%s_%s_requestsServerSorting',
    async (dataField, gridOrder, expectedSortBy, expectedSortOrder) => {
      await render();

      grid().instance.clearSorting();
      grid().instance.columnOption(dataField, 'sortOrder', gridOrder);

      await vi.waitFor(() =>
        expect(lastQuery()).toMatchObject({ sortBy: expectedSortBy, sortOrder: expectedSortOrder }),
      );
    },
  );

  it('page_secondPage_requestsPageTwo', async () => {
    getDevices.mockImplementation(serverPage(45));
    await render();

    grid().instance.pageIndex(1);

    await vi.waitFor(() => expect(lastQuery()).toMatchObject({ page: 2, pageSize: 20 }));
  });

  it('page_pageSizeGrownFrom20To50_showsRows0To49WithoutDuplicates', async () => {
    getDevices.mockImplementation(serverPage(300));
    await render();

    grid().instance.pageSize(50);

    await vi.waitFor(() => expect(visibleInventoryNumbers()).toEqual(inventoryNumbers(0, 50)));
  });

  it('page_sequenceOfPageAndSizeChanges_alwaysShowsTheRightRows', async () => {
    getDevices.mockImplementation(serverPage(300));
    await render();
    const steps: [() => void, number, number][] = [
      [() => grid().instance.pageSize(50), 0, 50],
      [() => grid().instance.pageIndex(2), 100, 50],
      [() => grid().instance.pageSize(100), 200, 100],
      [() => grid().instance.pageIndex(1), 100, 100],
      [() => grid().instance.pageSize(10), 10, 10],
      [() => grid().instance.pageIndex(4), 40, 10],
      [() => grid().instance.pageSize(20), 80, 20],
    ];

    for (const [change, firstRow, rowCount] of steps) {
      change();

      await vi.waitFor(() =>
        expect(visibleInventoryNumbers()).toEqual(inventoryNumbers(firstRow, rowCount)),
      );
    }
  });

  it('hubChange_burstOfChanges_reloadsOnce', async () => {
    await render();
    const callsBefore = getDevices.mock.calls.length;

    hubChanges.next();
    hubChanges.next();
    hubChanges.next();

    await vi.waitFor(() => expect(getDevices.mock.calls.length).toBe(callsBefore + 1));
    await new Promise((resolve) => setTimeout(resolve, 500));
    expect(getDevices.mock.calls.length).toBe(callsBefore + 1);
  });

  it('hubChange_newTotal_updatesRecordCount', async () => {
    await render();
    getDevices.mockImplementation(() => of(page(2)));

    hubChanges.next();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(text()).toContain('Записей: 2 из 2000');
    });
  });

  it('render_registryFull_disablesAddButton', async () => {
    getDevices.mockImplementation(() => of(page(2000)));
    await render();

    await vi.waitFor(() => {
      fixture.detectChanges();
      const button = fixture.nativeElement.querySelector(ADD_BUTTON);
      expect(button.classList).toContain('dx-state-disabled');
    });
  });

  it('render_registryNotFull_enablesAddButton', async () => {
    getDevices.mockImplementation(() => of(page(1999)));
    await render();

    await vi.waitFor(() => {
      fixture.detectChanges();
      const button = fixture.nativeElement.querySelector(ADD_BUTTON);
      expect(button.textContent).toContain('Добавить технику');
      expect(button.classList).not.toContain('dx-state-disabled');
    });
  });

  it('render_loadFails_showsErrorNotification', async () => {
    getDevices.mockImplementation(() => throwError(() => new Error('offline')));
    await render();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith('Не удалось загрузить список техники'),
    );
  });

  it('addButton_click_opensEmptyCreateDialog', async () => {
    await render();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector(ADD_BUTTON)).not.toBeNull();
    });
    fixture.nativeElement.querySelector(ADD_BUTTON).click();
    fixture.detectChanges();

    await vi.waitFor(() => expect(document.body.textContent).toContain('Новая техника'));
  });

  it('row_doubleClick_opensEditDialogForThatDevice', async () => {
    await render();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.dx-data-row')).not.toBeNull(),
    );

    await userEvent.dblClick(fixture.nativeElement.querySelector('.dx-data-row td'));
    fixture.detectChanges();

    await vi.waitFor(() => expect(document.body.textContent).toContain('Редактирование техники'));
  });

  it('row_editButton_opensEditDialogForThatDevice', async () => {
    await render();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.dx-data-row .dx-icon-edit')).not.toBeNull(),
    );

    fixture.nativeElement.querySelector('.dx-data-row .dx-icon-edit').click();
    fixture.detectChanges();

    await vi.waitFor(() => expect(document.body.textContent).toContain('Редактирование техники'));
  });

  it('row_deleteConfirmed_deletesDeviceAndReloads', async () => {
    await render();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash')).not.toBeNull(),
    );
    const callsBefore = getDevices.mock.calls.length;

    fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash').click();
    await vi.waitFor(() => expect(document.querySelector('.dx-dialog-button')).not.toBeNull());
    expect(document.body.textContent).toContain('Удалить технику «PC1»?');
    (document.querySelector('.dx-dialog-button') as HTMLElement).click();

    await vi.waitFor(() => expect(deleteDevice).toHaveBeenCalledWith('d1'));
    await vi.waitFor(() => expect(getDevices.mock.calls.length).toBeGreaterThan(callsBefore));
    expect(notifications.success).toHaveBeenCalledWith('Техника «PC1» удалена');
  });

  it('row_deleteCancelled_keepsDevice', async () => {
    await render();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash')).not.toBeNull(),
    );

    fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash').click();
    await vi.waitFor(() => expect(document.querySelectorAll('.dx-dialog-button').length).toBe(2));
    (document.querySelectorAll('.dx-dialog-button')[1] as HTMLElement).click();
    await new Promise((resolve) => setTimeout(resolve, 100));

    expect(deleteDevice).not.toHaveBeenCalled();
  });

  it('row_deleteFails_showsErrorNotification', async () => {
    deleteDevice.mockImplementation(() => throwError(() => new Error('offline')));
    await render();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash')).not.toBeNull(),
    );

    fixture.nativeElement.querySelector('.dx-data-row .dx-icon-trash').click();
    await vi.waitFor(() => expect(document.querySelector('.dx-dialog-button')).not.toBeNull());
    (document.querySelector('.dx-dialog-button') as HTMLElement).click();

    await vi.waitFor(() =>
      expect(notifications.error).toHaveBeenCalledWith('Не удалось удалить запись'),
    );
  });
});
